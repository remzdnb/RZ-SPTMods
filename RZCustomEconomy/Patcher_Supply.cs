// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces
// ReSharper disable InvertIf

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Runs after all assort injections (AutoRouting & ManualOffers are both at RagfairCallbacks - 2).
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class SupplyPatcher(
    ILogger<SupplyPatcher> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);
    private readonly SupplyConfig _supplyConfig = configLoader.Load<SupplyConfig>(SupplyConfig.FileName);

    public Task OnLoad()
    {
        if (!_masterConfig.EnableSupplyConfig)
            return Task.CompletedTask;

        PatchRestockTimes();
        PatchStockMultipliers();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // PatchRestockTimes
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void PatchRestockTimes()
    {
        if (!_supplyConfig.EnableRestockTimes || _supplyConfig.RestockTimes.Count == 0)
            return;

        foreach (var (traderName, seconds) in _supplyConfig.RestockTimes)
        {
            var traderId = TraderIds.FromName(traderName);
            if (traderId is null)
            {
                logger.LogWarning("[RZCustomEconomy] Supply/RestockTimes: unknown trader '{Name}' — skipping.", traderName);
                continue;
            }

            var existing = _traderConfig.UpdateTime.FirstOrDefault(u => u.TraderId == traderId);
            if (existing is not null)
            {
                existing.Seconds = new MinMax<int>(seconds, seconds);
            }
            else
            {
                _traderConfig.UpdateTime.Add(new UpdateTime
                {
                    Name = traderName,
                    TraderId = traderId,
                    Seconds = new MinMax<int>(seconds, seconds)
                });
            }

            // Clamp NextResupply if the new interval is shorter than the current timer.
            var trader = databaseService.GetTraders().GetValueOrDefault(traderId);
            if (trader is not null)
            {
                var newNextResupply = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;
                if (trader.Base.NextResupply > newNextResupply)
                    trader.Base.NextResupply = newNextResupply;
            }
        }

        if (configLoader.Load<MasterConfig>(MasterConfig.FileName).EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] Supply/RestockTimes: {Count} override(s) applied.", _supplyConfig.RestockTimes.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // PatchStockMultipliers
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void PatchStockMultipliers()
    {
        if (!_supplyConfig.EnableStockMultipliers)
            return;

        if (!_supplyConfig.StockMultipliers.EnableByTrader && !_supplyConfig.StockMultipliers.EnableByCategory)
        {
            logger.LogWarning("[RZCustomEconomy] Supply/StockMultipliers: both EnableByTrader and EnableByCategory are false — nothing to do.");
            return;
        }

        var traders = databaseService.GetTraders();
        var handbook = databaseService.GetTables().Templates?.Handbook;

        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] Supply/StockMultipliers: handbook is null — skipping.");
            return;
        }

        var tplToCategory = handbook.Items.ToDictionary(
            i => i.Id.ToString(),
            i => i.ParentId.ToString(),
            StringComparer.OrdinalIgnoreCase
        );

        var catToParent = handbook.Categories.ToDictionary(
            c => c.Id.ToString(),
            c => c.ParentId?.ToString(),
            StringComparer.OrdinalIgnoreCase
        );

        var traderMultipliers = _supplyConfig.StockMultipliers.ByTrader
            .Select(kvp => (Id: TraderIds.FromName(kvp.Key), Mult: kvp.Value))
            .Where(t => t.Id is not null)
            .ToDictionary(t => t.Id!, t => t.Mult, StringComparer.OrdinalIgnoreCase);

        int patched = 0, skippedUnlimited = 0, skippedMult1 = 0, skippedNullUpd = 0;

        foreach (var (traderId, trader) in traders)
        {
            var assort = trader.Assort;
            if (assort?.Items is null || assort.Items.Count == 0)
                continue;

            var rootItems = assort.Items.Where(i => i.ParentId == "hideout").ToList();

            var traderMult = 1.0;
            if (_supplyConfig.StockMultipliers.EnableByTrader && traderMultipliers.TryGetValue(traderId.ToString(), out var tm))
                traderMult = tm;

            var traderPatched = 0;
            var traderSkippedUnlimited = 0;
            var traderSkippedMult1 = 0;
            var traderSkippedNullUpd = 0;

            foreach (var item in rootItems)
            {
                var upd = item.Upd;

                if (upd is null)
                {
                    skippedNullUpd++;
                    traderSkippedNullUpd++;
                    continue;
                }

                // BuyRestrictionMax is the real purchasable stock. Null or 0 = truly unlimited.
                var buyMax = upd.BuyRestrictionMax;
                if (buyMax is null or 0)
                {
                    skippedUnlimited++;
                    traderSkippedUnlimited++;
                    continue;
                }

                var categoryMult = 1.0;
                if (_supplyConfig.StockMultipliers.EnableByCategory && tplToCategory.TryGetValue(item.Template.ToString(), out var categoryId))
                    categoryMult = ResolveCategoryMultiplier(categoryId, _supplyConfig.StockMultipliers.ByCategory, catToParent);

                var finalMult = traderMult * categoryMult;

                if (Math.Abs(finalMult - 1.0) < 0.0001)
                {
                    skippedMult1++;
                    traderSkippedMult1++;
                    continue;
                }

                var newMax = Math.Max(1, (int)Math.Round(buyMax.Value * finalMult));

                upd.BuyRestrictionMax = newMax;
                upd.BuyRestrictionCurrent = 0;

                patched++;
                traderPatched++;
            }

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation(
                    "[RZCustomEconomy] Supply/StockMultipliers: trader={Id} rootItems={Total} patched={P} skipped(unlimited={U} mult1={M} nullUpd={N})",
                    traderId, rootItems.Count, traderPatched, traderSkippedUnlimited, traderSkippedMult1, traderSkippedNullUpd
                );
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ResolveCategoryMultiplier
    // ─────────────────────────────────────────────────────────────────────────

    private static double ResolveCategoryMultiplier(
        string categoryId,
        Dictionary<string, double> byCategory,
        Dictionary<string, string?> catToParent
    )
    {
        var current = categoryId;
        while (current is not null)
        {
            if (byCategory.TryGetValue(current, out var mult))
                return mult;

            catToParent.TryGetValue(current, out current!);
        }

        return 1.0;
    }
}
