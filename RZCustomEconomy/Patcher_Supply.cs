// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

// Runs after all assort injections (AutoRouting + ManualOffers are both at RagfairCallbacks - 2).
[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class SupplyPatcher(
    ILogger<SupplyPatcher> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    ConfigLoader configLoader
) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());

        if (!masterConfig.EnableSupplyConfig)
            return Task.CompletedTask;

        var config = configLoader.Load<SupplyConfig>(SupplyConfig.FileName, Assembly.GetExecutingAssembly());

        PatchRestockTimes(config);
        PatchStockMultipliers(config);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PatchRestockTimes
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchRestockTimes(SupplyConfig config)
    {
        if (!config.EnableRestockTimes || config.RestockTimes.Count == 0)
            return;

        var traderConfig = configServer.GetConfig<TraderConfig>();

        foreach ((string traderId, int seconds) in config.RestockTimes)
        {
            // Fence is managed by FencePatcher.
            if (traderId == Traders.FENCE)
                continue;

            var existing = traderConfig.UpdateTime.FirstOrDefault(u => u.TraderId == traderId);
            if (existing is not null)
            {
                existing.Seconds = new MinMax<int>(seconds, seconds);
            }
            else
            {
                traderConfig.UpdateTime.Add(new UpdateTime
                {
                    Name = traderId,
                    TraderId = traderId,
                    Seconds = new MinMax<int>(seconds, seconds)
                });
            }

            var trader = databaseService.GetTraders().GetValueOrDefault(traderId);
            if (trader is not null)
            {
                var newNextResupply = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + seconds;
                if (trader.Base.NextResupply > newNextResupply)
                    trader.Base.NextResupply = newNextResupply;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PatchStockMultipliers
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchStockMultipliers(SupplyConfig config)
    {
        var stockConfig = config.StockMultipliers;
        if (!stockConfig.EnableByTrader && !stockConfig.EnableByCategory)
        {
            logger.LogWarning("[RZCustomEconomy] Supply/StockMultipliers: both EnableByTrader and EnableByCategory are false — nothing to do.");
            return;
        }

        var handbook = databaseService.GetTables().Templates?.Handbook;
        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] Supply/StockMultipliers: handbook is null — skipping.");
            return;
        }

        var traders = databaseService.GetTraders();

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

        var traderMultipliers = stockConfig.ByTrader
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase);

        int patched = 0, skippedUnlimited = 0, skippedMult1 = 0, skippedNullUpd = 0;
        var devLogs = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly()).EnableDevLogs;

        foreach ((MongoId traderId, Trader trader) in traders)
        {
            // Fence is managed by FencePatcher
            if (traderId == Traders.FENCE)
                continue;

            var assort = trader.Assort;
            if (assort?.Items is null || assort.Items.Count == 0)
                continue;

            var rootItems = assort.Items.Where(i => i.ParentId == "hideout").ToList();

            var traderMult = 1.0;
            if (stockConfig.EnableByTrader && traderMultipliers.TryGetValue(traderId.ToString(), out var tm))
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

                var buyMax = upd.BuyRestrictionMax;
                if (buyMax is null or 0)
                {
                    skippedUnlimited++;
                    traderSkippedUnlimited++;
                    continue;
                }

                var categoryMult = 1.0;
                if (stockConfig.EnableByCategory && tplToCategory.TryGetValue(item.Template.ToString(), out var categoryId))
                    categoryMult = ResolveCategoryMultiplier(categoryId, stockConfig.ByCategory, catToParent);

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
        }

        if (devLogs)
            logger.LogInformation(
                "[RZCustomEconomy] Supply/StockMultipliers done: {Patched} patched, {Unlimited} unlimited, {Mult1} mult=1, {NullUpd} nullUpd.",
                patched, skippedUnlimited, skippedMult1, skippedNullUpd
            );
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
