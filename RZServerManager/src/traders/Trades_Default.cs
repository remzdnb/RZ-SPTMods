// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.Traders;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// DEFAULT TRADES PATCHER  (RagfairCallbacks - 3)
//
// Must run at RagfairCallbacks - 3 so the slate is clean before injection patchers run at - 2.
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 3)]
public class DefaultTradesPatcher(
    ILogger<DefaultTradesPatcher> logger,
    DatabaseService databaseService,
    ConfigLoader configLoader,
    AssortUtilities assortUtilities
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>();
    private readonly DefaultTradesConfig _defaultTradesConfig = configLoader.Load<DefaultTradesConfig>();

    public Task OnLoad()
    {
        if (_defaultTradesConfig.ClearAllDefaultTrades)
        {
            ClearDefaultAssorts();
            return Task.CompletedTask;
        }

        ApplyBlacklist();
        ReplaceBarterTrades(assortUtilities);
        ApplyPriceMultipliers();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ClearDefaultAssorts
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ClearDefaultAssorts()
    {
        foreach ((MongoId id, Trader trader) in databaseService.GetTraders())
        {
            if (id == SPTarkov.Server.Core.Models.Enums.Traders.FENCE)
                continue;

            trader.Assort = new TraderAssort {
                Items                = new List<Item>(),
                BarterScheme         = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems      = new Dictionary<MongoId, int>(),
                NextResupply         = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
            };
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZSM] DefaultTrades: all trader assorts cleared.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ApplyBlacklist
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyBlacklist()
    {
        if (_defaultTradesConfig.Blacklist.Count == 0)
            return;

        var blacklistTpls = _defaultTradesConfig.Blacklist.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var traders       = databaseService.GetTraders();
        var removed       = 0;

        foreach (var (_, trader) in traders)
        {
            var assort = trader.Assort;
            if (assort?.Items is null)
                continue;

            var toRemove = assort.Items
                .Where(i => i.ParentId == "hideout" && blacklistTpls.Contains(i.Template.ToString()))
                .ToList();

            foreach (var root in toRemove)
            {
                assort.Items.RemoveAll(i => i.Id == root.Id || i.ParentId == root.Id);
                assort.BarterScheme.Remove(root.Id);
                assort.LoyalLevelItems.Remove(root.Id);
                removed++;
            }
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZSM] DefaultTrades/Blacklist: {Count} item(s) removed from trader assorts.", removed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ApplyPriceMultipliers
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ApplyPriceMultipliers()
    {
        if (_defaultTradesConfig.PriceMultipliers.Count == 0)
            return;

        HashSet<string> currencyTpls = [
            ItemTpl.MONEY_ROUBLES,
            ItemTpl.MONEY_DOLLARS,
            ItemTpl.MONEY_EUROS,
        ];

        var traders = databaseService.GetTraders();
        var patched = 0;

        foreach (var (traderId, multiplier) in _defaultTradesConfig.PriceMultipliers)
        {
            if (multiplier == 1.0)
                continue;

            if (!traders.TryGetValue(traderId, out var trader))
            {
                logger.LogWarning("[RZCustomEconomy] DefaultTrades/PriceMultipliers: trader '{Id}' not found : skipping.", traderId);
                continue;
            }

            var assort = trader.Assort;
            if (assort?.BarterScheme is null)
                continue;

            foreach (var schemes in assort.BarterScheme.Values)
            {
                foreach (var scheme in schemes)
                {
                    // Only touch schemes where every entry is a currency (pure cash offers).
                    if (!scheme.All(s => currencyTpls.Contains(s.Template.ToString())))
                        continue;

                    foreach (var entry in scheme) {
                        entry.Count = Math.Max(1, Math.Round((entry.Count ?? 0) * multiplier));
                    }

                    patched++;
                }
            }
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZCustomEconomy] DefaultTrades/PriceMultipliers: {Count} offer(s) repriced.", patched);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ReplaceBarterTrades
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void ReplaceBarterTrades(AssortUtilities assortUtilities)
    {
        if (_defaultTradesConfig.NoBarterTraders.Count == 0)
            return;

        var traders  = databaseService.GetTraders();
        var handbook = databaseService.GetTables().Templates?.Handbook;

        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] DefaultTrades/NoBarterTrades: handbook is null : skipping.");
            return;
        }

        HashSet<string> currencyTpls = [
            ItemTpl.MONEY_ROUBLES,
            ItemTpl.MONEY_DOLLARS,
            ItemTpl.MONEY_EUROS,
        ];

        var handbookItems    = handbook.Items.ToDictionary(e => e.Id.ToString(), e => (double)(e.Price ?? 0));
        var dollarPriceInRub = handbookItems.GetValueOrDefault(ItemTpl.MONEY_DOLLARS.ToString(), 0.0);
        var euroPriceInRub   = handbookItems.GetValueOrDefault(ItemTpl.MONEY_EUROS.ToString(), 0.0);

        if (dollarPriceInRub <= 0)
            logger.LogWarning("[RZCustomEconomy] DefaultTrades/NoBarterTrades: handbook price for USD is 0 or missing — USD conversion will fallback to roubles.");
        if (euroPriceInRub <= 0)
            logger.LogWarning("[RZCustomEconomy] DefaultTrades/NoBarterTrades: handbook price for EUR is 0 or missing — EUR conversion will fallback to roubles.");

        var converted = 0;
        var skipped   = 0;

        foreach (var (traderId, traderEntry) in _defaultTradesConfig.NoBarterTraders)
        {
            if (!traderEntry.Enabled)
                continue;

            if (!traders.TryGetValue(traderId, out var trader))
            {
                logger.LogWarning("[RZCustomEconomy] DefaultTrades/NoBarterTrades: trader '{Id}' not found : skipping.", traderId);
                continue;
            }

            var excludedTpls = traderEntry.ExcludedBarterTpls.ToHashSet();
            var assort       = trader.Assort;

            if (assort?.BarterScheme is null)
                continue;

            foreach (var (assortId, schemes) in assort.BarterScheme)
            {
                foreach (var scheme in schemes)
                {
                    if (scheme.All(s => currencyTpls.Contains(s.Template.ToString())))
                        continue;

                    if (excludedTpls.Count > 0 && scheme.All(s => excludedTpls.Contains(s.Template.ToString())))
                        continue;

                    var rootItem = assort.Items.FirstOrDefault(i => i.Id == assortId);
                    if (rootItem is null)
                    {
                        skipped++;
                        continue;
                    }

                    var priceRub = assortUtilities.GetTotalHandbookPrice(rootItem.Template, handbookItems);
                    if (priceRub <= 0)
                    {
                        skipped++;
                        logger.LogWarning("[RZCustomEconomy] DefaultTrades/NoBarterTrades: no handbook price for '{Tpl}' : skipping.",
                            rootItem.Template);
                        continue;
                    }

                    MongoId currencyTpl;
                    double  finalPrice;

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
                            currencyTpl = ItemTpl.MONEY_ROUBLES;
                            finalPrice  = priceRub;
                            break;
                    }

                    scheme.Clear();
                    scheme.Add(new BarterScheme {
                        Template = currencyTpl,
                        Count    = finalPrice,
                    });
                    converted++;
                }
            }
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZCustomEconomy] DefaultTrades/NoBarterTrades: {Converted} barter(s) converted to cash, {Skipped} skipped.", converted, skipped);
        }
    }
}
