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
        if (!_masterConfig.EnableHandbookPricesConfig || _masterConfig.HandbookPrices.Count == 0)
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
    private readonly MasterConfig _defaultTradesConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        ClearDefaultAssorts();
        DisableFleaMarket();
        DisableFenceOffers();
        ReplaceBarterTrades();

        return Task.CompletedTask;
    }

    public void ClearDefaultAssorts()
    {
        // Clear all default assorts here rather than earlier, so any assorts added by other mods are also wiped before we inject our own.

        if (_masterConfig.EnableDefaultTrades)
            return;

        foreach (var (id, trader) in databaseService.GetTraders())
        {
            trader.Assort = new TraderAssort {
                Items = new List<Item>(),
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>(),
                NextResupply = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
            };
        }

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] All trader assorts cleared.");
    }

    public void DisableFleaMarket()
    {
        if (!_masterConfig.DisableFleaMarket)
            return;

        var ragfairConfig = configServer.GetConfig<RagfairConfig>();

        // Block initial dynamic offer generation by zeroing all offer counts.
        foreach (var key in ragfairConfig.Dynamic.OfferItemCount.Keys.ToList()) {
            ragfairConfig.Dynamic.OfferItemCount[key] = new MinMax<int> { Min = 0, Max = 0 };
        }

        // Block regeneration of expired offers.
        ragfairConfig.Dynamic.ExpiredOfferThreshold = int.MaxValue;

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] Flea market offers removed.");
    }

    public void DisableFenceOffers()
    {
        if (_masterConfig.EnableFenceTrades)
            return;

        var fence = configServer.GetConfig<TraderConfig>().Fence;
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

    public void ReplaceBarterTrades()
    {
        if (!_masterConfig.EnableDefaultTrades)
        {
            logger.LogWarning("[RZCustomEconomy] NoBarterTrades: EnableDefaultAssorts is false — skipping, nothing to convert.");
            return;
        }

        var config = configLoader.Load<DefaultTradesConfig>(DefaultTradesConfig.FileName);

        if (config.NoBarterTraders.Count == 0)
            return;

        var traders = databaseService.GetTraders();
        var handbook = databaseService.GetTables().Templates?.Handbook;

        if (handbook is null) {
            logger.LogWarning("[RZCustomEconomy] NoBarterTrades: handbook is null — skipping.");
            return;
        }

        HashSet<string> currencyTpls = [
            ItemTpl.MONEY_ROUBLES,
            ItemTpl.MONEY_DOLLARS,
            ItemTpl.MONEY_EUROS,
        ];

        // Read currency exchange rates from handbook prices.
        // Handbook stores dollar and euro prices in roubles — dividing gives us the rub→currency rate.
        var handbookItems = handbook.Items.ToDictionary(e => e.Id.ToString(), e => (double)(e.Price ?? 0));

        var dollarPriceInRub = handbookItems.GetValueOrDefault(ItemTpl.MONEY_DOLLARS.ToString(), 0.0);
        var euroPriceInRub   = handbookItems.GetValueOrDefault(ItemTpl.MONEY_EUROS.ToString(), 0.0);

        if (dollarPriceInRub <= 0)
            logger.LogWarning("[RZCustomEconomy] NoBarterTrades: handbook price for USD is 0 or missing — USD conversion will fallback to roubles.");
        if (euroPriceInRub <= 0)
            logger.LogWarning("[RZCustomEconomy] NoBarterTrades: handbook price for EUR is 0 or missing — EUR conversion will fallback to roubles.");

        // Build a lookup: traderId → entry (currency + exclusion list).
        var traderEntryMap = config.NoBarterTraders
            .Select(kvp => (Id: TraderIds.FromName(kvp.Key), Entry: kvp.Value))
            .Where(t => t.Id is not null)
            .ToDictionary(t => t.Id!.ToString(), t => t.Entry);

        var converted = 0;
        var skipped = 0;

        foreach (var (traderId, trader) in traders)
        {
            if (!traderEntryMap.TryGetValue(traderId.ToString(), out var traderEntry))
                continue;

            if (!traderEntry.Enabled)
                continue;

            var excludedTpls = traderEntry.ExcludedBarterTpls.ToHashSet();

            var assort = trader.Assort;
            if (assort?.BarterScheme is null) continue;

            foreach (var (assortId, schemes) in assort.BarterScheme)
            {
                foreach (var scheme in schemes)
                {
                    // Already a cash-only scheme — leave it alone.
                    var isCashOnly = scheme.All(s => currencyTpls.Contains(s.Template.ToString()));
                    if (isCashOnly)
                        continue;

                    // If every item in the scheme is in the exclusion list, skip it.
                    if (excludedTpls.Count > 0 && scheme.All(s => excludedTpls.Contains(s.Template.ToString())))
                        continue;

                    // Find the root item for this assort to get its handbook price.
                    var rootItem = assort.Items.FirstOrDefault(i => i.Id == assortId);
                    if (rootItem is null) {
                        skipped++;
                        continue;
                    }

                    if (!handbookItems.TryGetValue(rootItem.Template.ToString(), out var priceRub) || priceRub <= 0) {
                        skipped++;
                        logger.LogWarning("[RZCustomEconomy] NoBarterTrades: no handbook price for '{Tpl}' — skipping.", rootItem.Template);
                        continue;
                    }

                    // Convert rouble price to the target currency.
                    MongoId currencyTpl;
                    double finalPrice;

                    switch (traderEntry.Currency)
                    {
                        case TradeCurrency.Usd when dollarPriceInRub > 0:
                            currencyTpl = ItemTpl.MONEY_DOLLARS;
                            finalPrice  = Math.Max(1, Math.Round(priceRub / dollarPriceInRub));
                            break;

                        case TradeCurrency.Eur when euroPriceInRub > 0:
                            currencyTpl = ItemTpl.MONEY_EUROS;
                            finalPrice  = Math.Max(1, Math.Round(priceRub / euroPriceInRub));
                            break;

                        default:
                            // Rub, or fallback if exchange rate is missing.
                            currencyTpl = ItemTpl.MONEY_ROUBLES;
                            finalPrice  = priceRub;
                            break;
                    }

                    scheme.Clear();
                    scheme.Add(new BarterScheme {
                        Template = currencyTpl,
                        Count = finalPrice
                    });
                    converted++;
                }
            }
        }

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] NoBarterTrades: {Converted} barter(s) converted to cash, {Skipped} skipped.", converted, skipped);
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// RAGFAIRCALLBACKS - 1
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class MasterPatchRagfairCallbacksMinusOne(
    ILogger<MasterPatcherRagfairCallbacksMinusTwo> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

    public Task OnLoad()
    {
        ApplyUserBlacklistToDefaultAssorts();
        ExamineAllItems();
        RemoveEmptyTraders();

        return Task.CompletedTask;
    }

    public void ApplyUserBlacklistToDefaultAssorts()
    {
        if (!_masterConfig.EnableDefaultTrades)
            return;

        var userBlacklist = _masterConfig.UserBlacklist;

        if (!userBlacklist.ApplyToDefaultAssorts || userBlacklist.Items.Count == 0)
            return;

        var blacklistTpls = userBlacklist.Items.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var traders = databaseService.GetTraders();

        var removed = 0;

        foreach (var (_, trader) in traders)
        {
            var assort = trader.Assort;
            if (assort?.Items is null)
                continue;

            // Find root items whose tpl is blacklisted.
            var toRemove = assort.Items
                .Where(i => i.ParentId == "hideout" && blacklistTpls.Contains(i.Template.ToString()))
                .ToList();

            foreach (var root in toRemove)
            {
                // Remove root + all children.
                assort.Items.RemoveAll(i => i.Id == root.Id || i.ParentId == root.Id);
                assort.BarterScheme.Remove(root.Id);
                assort.LoyalLevelItems.Remove(root.Id);
                removed++;
            }
        }

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] UserBlacklist: {Count} item(s) removed from default assorts.", removed);
    }

    public void ExamineAllItems()
    {
        if (!_masterConfig.AllItemsExamined)
            return;

        var profiles = databaseService.GetProfileTemplates();
        var allItems = databaseService.GetTables().Templates?.Items?.Keys.ToHashSet();
        var staticBlacklist = new HashSet<MongoId>(_masterConfig.StaticBlacklist.Select(tpl => new MongoId(tpl)));

        foreach (var (_, edition) in profiles)
        {
            foreach (var side in new[] { edition.Usec, edition.Bear })
            {
                var character = side?.Character;
                if (character is null)
                    continue;

                character.Encyclopedia ??= new Dictionary<MongoId, bool>();
                foreach (var tpl in allItems!)
                {
                    if (staticBlacklist.Contains(tpl))
                        continue;

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

