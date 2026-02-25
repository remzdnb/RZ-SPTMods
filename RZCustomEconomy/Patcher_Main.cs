// RemzDNB - 2026
// ReSharper disable InvertIf
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace RZCustomEconomy;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// LOAD ORDER
//
// All patchers run just before RagfairCallbacks to ensure assorts are fully built before the flea market indexes them.
//
// RagfairCallbacks - 3 : Clear default/fence assorts.
// Must run first — we want a clean slate before injecting anything. Running this after injections would wipe our own offers.
//
// RagfairCallbacks - 2 : Inject offers (AutoRoutingPatcher, ManualOffersPatcher).
// Runs on the clean slate left by the previous step. Auto-routing maps handbook categories → traders. Manual offers are injected on top.
//
// RagfairCallbacks - 1 : Sanity check (RemoveEmptyTraders)
// Removes traders with no assorts at all, just before ragfair boots up. Avoids empty trader entries being indexed by the flea market.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// POSTDBMOADLOADER + 1
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class MasterPatcherPostDbModLoader(
    ILogger<MasterPatcherPostDbModLoader> logger,
    DatabaseService databaseService,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        PatchHandbookPrices();
        UnlockAllTraders();

        return Task.CompletedTask;
    }

    private void PatchHandbookPrices()
    {
        if (_masterConfig.HandbookPrices.Count == 0)
            return;

        var handbook = databaseService.GetTables().Templates?.Handbook;
        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] Handbook is null, cannot patch prices.");
            return;
        }

        foreach (var (tpl, price) in _masterConfig.HandbookPrices)
        {
            var entry = handbook.Items.FirstOrDefault(i => i.Id.ToString() == tpl);
            if (entry is null)
            {
                logger.LogWarning("[RZCustomEconomy] Handbook entry '{Tpl}' not found, skipping.", tpl);
                continue;
            }

            entry.Price = price;
        }
    }

    private void UnlockAllTraders()
    {
        var traders = databaseService.GetTraders();

        if (!_masterConfig.UnlockAllTraders)
            return;

        foreach (var (_, trader) in traders)
        {
            trader.Base.UnlockedByDefault = true;
        }

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] All traders unlocked.");
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// RAGFAIRCALLBACKS - 3
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 3)]
public class MasterPatcherRagfairCallbacksMinusTwo(
    ILogger<MasterPatcherRagfairCallbacksMinusTwo> logger,
    DatabaseService databaseService,
    FenceService fenceService,
    ConfigServer configServer,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        var traders = databaseService.GetTraders();
        var traderConfig = configServer.GetConfig<TraderConfig>();
        var ragfairConfig = configServer.GetConfig<RagfairConfig>();

        // CLEAR DEFAULT ASSORTS
        // Clear all default assorts here rather than earlier, so any assorts added by other mods are also wiped before we inject our own.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (_masterConfig.ClearDefaultAssorts)
        {
            foreach (var (id, trader) in traders)
            {
                trader.Assort = new TraderAssort
                {
                    Items = new List<Item>(),
                    BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                    LoyalLevelItems = new Dictionary<MongoId, int>(),
                    NextResupply = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
                };
            }

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation("[RZCustomEconomy] All trader assorts cleared.");
        }

        // REMOVE FLEA MARKET OFFERS
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (_masterConfig.DisableFleaMarket)
        {
            // Block initial dynamic offer generation by zeroing all offer counts.
            foreach (var key in ragfairConfig.Dynamic.OfferItemCount.Keys.ToList()) {
                ragfairConfig.Dynamic.OfferItemCount[key] = new MinMax<int> { Min = 0, Max = 0 };
            }

            // Block regeneration of expired offers.
            ragfairConfig.Dynamic.ExpiredOfferThreshold = int.MaxValue;

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation("[RZCustomEconomy] Flea market offers removed.");
        }

        // DISABLE FENCE OFFERS
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (_masterConfig.ClearFenceAssorts)
        {
            var fence = traderConfig.Fence;
            fence.AssortSize = 0;
            fence.WeaponPresetMinMax = new MinMax<int> { Min = 0, Max = 0 };
            fence.EquipmentPresetMinMax = new MinMax<int> { Min = 0, Max = 0 };
            fence.DiscountOptions.AssortSize = 0;
            fence.DiscountOptions.WeaponPresetMinMax = new MinMax<int> { Min = 0, Max = 0 };
            fence.DiscountOptions.EquipmentPresetMinMax = new MinMax<int> { Min = 0, Max = 0 };

            fenceService.SetFenceAssort(
                new TraderAssort {
                    Items = [],
                    BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                    LoyalLevelItems = new Dictionary<MongoId, int>(),
                }
            );
            fenceService.SetFenceDiscountAssort(
                new TraderAssort {
                    Items = [],
                    BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                    LoyalLevelItems = new Dictionary<MongoId, int>(),
                }
            );

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation("[RZCustomEconomy] Fence offers disabled.");
        }

        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// RAGFAIRCALLBACKS - 1
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class MasterPatchRagfairCallbacksMinusOne(DatabaseService databaseService, ConfigServer configServer, ConfigLoader configLoader)
    : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);
    private readonly AutoRoutingConfig _autoRoutingConfig = configLoader.Load<AutoRoutingConfig>(AutoRoutingConfig.FileName);

    public Task OnLoad()
    {
        ExamineAllItems();
        RemoveEmptyTraders();

        return Task.CompletedTask;
    }

    public void ExamineAllItems()
    {
        // We do this here because it's late enough for modded items to be examined as well.

        if (!_masterConfig.AllItemsExamined)
            return;

        var profiles = databaseService.GetProfileTemplates();
        var allItems = databaseService.GetTables().Templates?.Items?.Keys.ToHashSet();
        var staticBlacklist = new HashSet<MongoId>(_autoRoutingConfig.StaticBlacklist.Select(tpl => new MongoId(tpl)));

        foreach (var (_, edition) in profiles)
        {
            foreach (var side in new[] { edition.Usec, edition.Bear })
            {
                var character = side?.Character;
                if (character is null)
                {
                    continue;
                }

                character.Encyclopedia ??= new Dictionary<MongoId, bool>();
                foreach (var tpl in allItems!)
                {
                    if (staticBlacklist.Contains(tpl))
                    {
                        // StaticBlacklist items are broken/non-functional — they should never appear as identified in the
                        // encyclopedia, even when AllItemsExamined is true.
                        continue;
                    }

                    character.Encyclopedia[tpl] = true;
                }
            }
        }
    }

    public void RemoveEmptyTraders()
    {
        // Removes traders with no assorts from the ragfair config to prevent ragfair from spamming errors when trying to generate offers
        // for empty traders. For this to work correctly, all patchers that modify trader assorts must run at RagfairCallbacks - 2 or
        // earlier.

        var traders = databaseService.GetTraders();
        var ragfairConfig = configServer.GetConfig<RagfairConfig>();

        foreach (var (id, trader) in traders)
        {
            if (trader.Assort?.Items is null || trader.Assort.Items.Count == 0)
            {
                ragfairConfig.Traders.Remove(id.ToString());
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// RAGFAIRCALLBACKS + 1
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks + 1)]
public class RagfairPostPatcher(
    ILogger<RagfairPostPatcher> logger,
    RagfairOfferHolder ragfairOfferHolder,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        // Purge all existing flea market offers.

        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

        if (!masterConfig.DisableFleaMarket)
            return Task.CompletedTask;

        var toRemove = ragfairOfferHolder.GetOffers().Where(o => o.User?.MemberType != MemberCategory.Trader).Select(o => o.Id).ToList();

        foreach (var id in toRemove) {
            ragfairOfferHolder.RemoveOffer(id);
        }

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] Flea market offers purged.");

        return Task.CompletedTask;
    }
}

