// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Services;

namespace RZCustomProfiles;

[Injectable]
public class ProfilesUtilities(
    ILogger<ProfilesUtilities> logger,
    DatabaseService databaseService,
    InventoryHelper inventoryHelper,
    AssortUtilities assortUtilities
)
{
    public void PatchSide(TemplateSide? side, string sideName, ProfileConfig config)
    {
        if (side?.Character is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} template character is null, skipping.", sideName);
            return;
        }

        ClearStartingItems(side, sideName, config);
        AddStashItems(side, sideName, config);
        ReplaceSecureContainer(side, sideName, config);

        ApplyLevel(side, config);
        ApplySkills(side, config, logger);
        ApplyHideoutLevels(side, sideName, config);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Starting items
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ClearStartingItems(TemplateSide side, string sideName, ProfileConfig config)
    {
        var inventory = side.Character!.Inventory;
        if (inventory?.Items is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} inventory is null, skipping item clear.", sideName);
            return;
        }

        // Clear stash.

        if (config.ClearStash)
        {
            var stashId = inventory.Stash?.ToString();
            if (stashId is null)
            {
                logger.LogWarning("[RZCustomProfiles] {Side} stash ID is null, skipping stash clear.", sideName);
            }
            else
            {
                inventory.Items.RemoveAll(i => i.ParentId == stashId);
            }
        }

        // Clear equipped items (except protected slot).

        if (config.ClearEquipment)
        {
            var equipmentId = inventory.Equipment?.ToString();
            if (equipmentId is null)
            {
                logger.LogWarning("[RZCustomProfiles] {Side} equipment ID is null, skipping equipment clear.", sideName);
            }
            else
            {
                // IDs des items pocket — leurs enfants sont protégés aussi mais leurs contenus non
                var pocketIds = inventory.Items
                    .Where(i => i.SlotId == "Pockets")
                    .Select(i => i.Id.ToString())
                    .ToHashSet();

                var equippedIds = new HashSet<string>();
                CollectEquippedIds(inventory.Items, equipmentId, equippedIds);

                inventory.Items.RemoveAll(i =>
                        equippedIds.Contains(i.Id.ToString()) &&
                        !MasterConfig.ProtectedSlots.Contains(i.SlotId ?? "") &&
                        !pocketIds.Contains(i.Id.ToString()) // garder l'item pocket lui-même
                        ||
                        pocketIds.Contains(i.ParentId ?? "") // mais virer ce qu'il y a dedans
                );
            }
        }
    }

    private void CollectEquippedIds(List<Item> items, string parentId, HashSet<string> result)
    {
        foreach (var item in items.Where(i => i.ParentId == parentId))
        {
            if (MasterConfig.ProtectedSlots.Contains(item.SlotId ?? "")) {
                continue;
            }

            result.Add(item.Id.ToString());
            CollectEquippedIds(items, item.Id.ToString(), result);
        }
    }

    private void AddStashItems(TemplateSide side, string sideName, ProfileConfig config)
    {
        var items = side.Character!.Inventory?.Items;
        if (items is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} inventory is null, skipping items replacement.", sideName);
            return;
        }

        var startingItemsCfg = config.AdditionalStartingItems;
        if (startingItemsCfg?.Enabled != true || startingItemsCfg.Items.Count == 0)
            return;

        var stashId = side.Character!.Inventory?.Stash;
        if (stashId is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} stash ID is null, skipping starting items injection.", sideName);
            return;
        }

        var (stashW, stashH) = GetStashSize(side);
        var containerMap = inventoryHelper.GetContainerMap(stashW, stashH, items, stashId.Value);

        foreach (var entry in startingItemsCfg.Items)
        {
            if (string.IsNullOrWhiteSpace(entry.Tpl) || entry.Count <= 0)
                continue;

            var template = databaseService.GetTables().Templates?.Items?.GetValueOrDefault(entry.Tpl);
            var stackMax = template?.Properties?.StackMaxSize ?? 1;
            var remaining = entry.Count;

            while (remaining > 0)
            {
                var stackCount = Math.Min(remaining, stackMax);
                var itemToAdd = assortUtilities.CreateRootItem(entry.Tpl, stackCount);

                var result = inventoryHelper.PlaceItemInContainer(containerMap, itemToAdd, stashId.Value.ToString());
                if (!result.Success.GetValueOrDefault(false))
                {
                    logger.LogWarning("[RZCustomProfiles] {Side} stash full, could not place '{Tpl}'.", sideName, entry.Tpl);
                    break;
                }

                items.AddRange(itemToAdd);
                remaining -= stackCount;
            }
        }
    }

    private void ReplaceSecureContainer(TemplateSide side, string sideName, ProfileConfig config)
    {
        if (config.SecureContainer == 0) {
            return;
        }

        var pouch = side.Character?.Inventory?.Items?.FirstOrDefault(i => i.SlotId == "SecuredContainer");
        if (pouch is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} SecuredContainer not found : skipping.", sideName);
            return;
        }

        if (config.SecureContainer == -1)
        {
            side.Character!.Inventory!.Items!.Remove(pouch);
            return;
        }

        if (!MasterConfig.SecureContainers.TryGetValue(config.SecureContainer, out var tpl))
        {
            logger.LogWarning("[RZCustomProfiles] Unknown SecureContainer index '{Index}' : skipping.", config.SecureContainer);
            return;
        }

        pouch.Template = tpl;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Hideout levels
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyHideoutLevels(TemplateSide side, string sideName, ProfileConfig config)
    {
        var startingLevels = config.HideoutStartingLevels;
        if (startingLevels is null || startingLevels.Count == 0)
            return;

        var areas = side.Character!.Hideout?.Areas;
        if (areas is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} hideout areas list is null, skipping.", sideName);
            return;
        }

        foreach (var (areaName, startingLevel) in startingLevels)
        {
            if (!Enum.TryParse<HideoutAreas>(areaName, ignoreCase: true, out var areaType))
            {
                logger.LogWarning("[RZCustomProfiles] Unknown hideout area '{Name}' : skipped.", areaName);
                continue;
            }

            var area = areas.FirstOrDefault(a => a.Type == areaType);
            if (area is null)
                continue;

            area.Level = startingLevel;
            area.Active = true;
            area.Constructing = false;
            area.CompleteTime = 0;
            area.PassiveBonusesEnabled = true;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Level
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyLevel(TemplateSide side, ProfileConfig config)
    {
        var info = side.Character?.Info;
        if (info is null) {
            return;
        }

        var expTable = databaseService.GetGlobals().Configuration.Exp.Level.ExperienceTable;

        if (config.MaxLevel) {
            info.Level      = expTable.Length;
            info.Experience = expTable.Sum(e => e.Experience);
            return;
        }

        if (config.StartingLevel is not { } targetLevel) {
            return;
        }

        targetLevel = Math.Clamp(targetLevel, 1, expTable.Length);
        info.Level      = targetLevel;
        info.Experience = expTable.Take(targetLevel).Sum(e => e.Experience);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Prestige level
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static void ApplyPrestige(TemplateSide side, ProfileConfig config, ILogger<ProfilesUtilities> logger)
    {
        if (config.StartingPrestigeLevel is not { } prestige)
        {
            logger.LogInformation("[RZCustomProfiles] ApplyPrestige: StartingPrestige is null, skipping.");
            return;
        }

        var info = side.Character?.Info;
        if (info is null)
        {
            logger.LogWarning("[RZCustomProfiles] ApplyPrestige: Info is null, skipping.");
            return;
        }

        logger.LogInformation("[RZCustomProfiles] ApplyPrestige: Info found, PrestigeLevel before = {Before}", info.PrestigeLevel);

        info.PrestigeLevel = Math.Max(0, prestige);

        logger.LogInformation("[RZCustomProfiles] ApplyPrestige: PrestigeLevel after = {After}", info.PrestigeLevel);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Skills
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static void ApplySkills(TemplateSide side, ProfileConfig config, ILogger<ProfilesUtilities> logger)
    {
        var skills = side.Character?.Skills?.Common;
        if (skills is null) {
            return;
        }

        if (config.MaxSkills)
        {
            foreach (var skill in skills) {
                skill.Progress = 5100f;
            }

            return;
        }

        if (config.SkillOverrides is not { Count: > 0 } overrides) {
            return;
        }

        foreach (var (skillId, level) in overrides)
        {
            var skill = skills.FirstOrDefault(s =>
                string.Equals(s.Id.ToString(), skillId, StringComparison.OrdinalIgnoreCase));

            if (skill is null) {
                logger.LogWarning("[RZCustomProfiles] Skill '{Id}' not found.", skillId);
                continue;
            }

            skill.Progress = Math.Clamp(level, 0, 51) * 100f;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Apply traders loyalty
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyTradersLoyalty(TemplateSide side, string sideName, ProfileConfig config)
    {
        if (config.TradersLoyalty is null || config.TradersLoyalty.Count == 0) {
            return;
        }

        var tradersInfo = side.Character?.TradersInfo;
        if (tradersInfo is null)
        {
            logger.LogWarning("[RZCustomProfiles] {Side} TradersInfo is null, skipping.", sideName);
            return;
        }

        foreach (var (traderId, traderConfig) in config.TradersLoyalty)
        {
            if (tradersInfo.TryGetValue(new MongoId(traderId), out var trader))
            {
                trader.Standing = traderConfig.Standing;
                trader.SalesSum = traderConfig.SalesSum;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private (int w, int h) GetStashSize(TemplateSide side)
    {
        var stashId = side.Character!.Inventory?.Stash;
        var stashItem = side.Character!.Inventory?.Items?.FirstOrDefault(i => i.Id == stashId?.ToString());
        var stashTemplate = stashItem is null ? null :
            databaseService.GetTables().Templates?.Items?.GetValueOrDefault(stashItem.Template.ToString());
        var grid = stashTemplate?.Properties?.Grids?.FirstOrDefault();
        return (grid?.Properties?.CellsH ?? 10, grid?.Properties?.CellsV ?? 68);
    }
}
