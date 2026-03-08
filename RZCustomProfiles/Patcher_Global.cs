// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace RZCustomProfiles;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class GlobalPatcher(ILogger<GlobalPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());
        var storage = databaseService.GetTemplates().CustomisationStorage;

        UnlockOutfits(masterConfig, storage);
        UnlockHideoutCustomizations(masterConfig, storage);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Outfits
    // ─────────────────────────────────────────────────────────────────────────

    private void UnlockOutfits(MasterConfig masterConfig, List<CustomisationStorage> storage)
    {
        if (!masterConfig.UnlockAllOutfits)
            return;

        var suits = databaseService.GetTraders().GetValueOrDefault(Traders.RAGMAN)?.Suits;
        if (suits is null)
        {
            logger.LogWarning("[RZCustomProfiles] Ragman suits is null : skipping outfit unlock.");
            return;
        }

        var storageIds = storage.Select(s => s.Id).ToHashSet();
        var added = 0;

        foreach (var suit in suits)
        {
            if (!storageIds.Add(suit.SuiteId))
                continue;

            storage.Add(new CustomisationStorage
            {
                Id = suit.SuiteId,
                Source = CustomisationSource.UNLOCKED_IN_GAME,
                Type = CustomisationType.SUITE,
            });
            added++;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Hideout customizations
    // ─────────────────────────────────────────────────────────────────────────

    private void UnlockHideoutCustomizations(MasterConfig masterConfig, List<CustomisationStorage> storage)
    {
        if (masterConfig.UnlockHideoutCustomizations is not { Count: > 0 } unlockConfig)
            return;

        // Build categoryId → customisationType for enabled entries only
        var categoryTypeMap = unlockConfig
            .Where(kvp => kvp.Value && MasterConfig.HideoutCategories.TryGetValue(kvp.Key, out _))
            .ToDictionary(
                kvp => MasterConfig.HideoutCategories[kvp.Key].CategoryId,
                kvp => MasterConfig.HideoutCategories[kvp.Key].CustomisationType
            );

        if (categoryTypeMap.Count == 0)
            return;

        var customizations = databaseService.GetTemplates().Customization;
        var storageIds = storage.Select(s => s.Id).ToHashSet();
        var added = 0;

        foreach (var (id, item) in customizations)
        {
            if (storageIds.Contains(id))
                continue;

            var type = FindCategoryType(item, customizations, categoryTypeMap);
            if (type is null)
                continue;

            storage.Add(new CustomisationStorage
            {
                Id = id,
                Source = CustomisationSource.UNLOCKED_IN_GAME,
                Type = type,
            });
            added++;
        }
    }

    private static string? FindCategoryType(
        CustomizationItem item,
        Dictionary<MongoId, CustomizationItem> all,
        Dictionary<string, string> categoryTypeMap)
    {
        var current = item.Parent;
        while (!string.IsNullOrEmpty(current))
        {
            if (categoryTypeMap.TryGetValue(current, out var type))
                return type;

            if (!all.TryGetValue(current, out var parent))
                break;

            current = parent.Parent;
        }
        return null;
    }
}
