// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace RZCustomEconomy;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// RAGFAIRCALLBACKS - 3
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 3)]
public class RagfairPatcher(
    ILogger<RagfairPatcher> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());

    public Task OnLoad()
    {
        if (!_masterConfig.EnableFleaMarketConfig)
            return Task.CompletedTask;

        var config = configLoader.Load<FleaMarketConfig>(FleaMarketConfig.FileName, Assembly.GetExecutingAssembly());

        DisableFleaMarket(config);
        ApplyDynamicItemFilter(config);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DisableFleaMarket
    // ─────────────────────────────────────────────────────────────────────────

    private void DisableFleaMarket(FleaMarketConfig config)
    {
        if (!config.Disable)
            return;

        var ragfairConfig = configServer.GetConfig<RagfairConfig>();

        foreach (var key in ragfairConfig.Dynamic.OfferItemCount.Keys.ToList())
            ragfairConfig.Dynamic.OfferItemCount[key] = new MinMax<int> { Min = 0, Max = 0 };

        ragfairConfig.Dynamic.ExpiredOfferThreshold = int.MaxValue;

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] Ragfair: dynamic offer generation disabled.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ApplyDynamicItemFilter
    //
    // DynamicForceDisable — sets CanSellOnRagfair = false on listed TPLs only.
    // DynamicForceEnable  — sets CanSellOnRagfair = true  on listed TPLs only.
    // Both are applied independently. Nothing outside the listed TPLs is touched.
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyDynamicItemFilter(FleaMarketConfig config)
    {
        if (config.DynamicForceDisable.Count == 0 && config.DynamicForceEnable.Count == 0)
            return;

        var items = databaseService.GetTables().Templates?.Items;
        if (items is null)
        {
            logger.LogWarning("[RZCustomEconomy] Ragfair: Templates.Items is null — skipping item filter.");
            return;
        }

        if (config.DynamicForceDisable.Count > 0)
        {
            var tpls = config.DynamicForceDisable.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var patched = 0;

            foreach (var (tpl, item) in items)
            {
                if (item.Properties is null || !tpls.Contains(tpl.ToString()))
                    continue;

                item.Properties.CanSellOnRagfair = false;
                patched++;
            }

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation("[RZCustomEconomy] Ragfair/ForceDisable: {Count} item(s) removed from dynamic offers.", patched);
        }

        if (config.DynamicForceEnable.Count > 0)
        {
            var tpls = config.DynamicForceEnable.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var patched = 0;

            foreach (var (tpl, item) in items)
            {
                if (item.Properties is null || !tpls.Contains(tpl.ToString()))
                    continue;

                item.Properties.CanSellOnRagfair = true;
                patched++;
            }

            if (_masterConfig.EnableDevLogs)
                logger.LogInformation("[RZCustomEconomy] Ragfair/ForceEnable: {Count} item(s) added to dynamic offers.", patched);
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
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());

    public Task OnLoad()
    {
        if (!_masterConfig.EnableFleaMarketConfig)
            return Task.CompletedTask;

        var config = configLoader.Load<FleaMarketConfig>(FleaMarketConfig.FileName, Assembly.GetExecutingAssembly());

        if (!config.Disable)
            return Task.CompletedTask;

        PurgeFleaMarketOffers();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PurgeFleaMarketOffers
    //
    // Removes any dynamic (non-trader) offers that were generated before
    // DisableFleaMarket had a chance to zero the offer counts.
    // ─────────────────────────────────────────────────────────────────────────

    private void PurgeFleaMarketOffers()
    {
        var toRemove = ragfairOfferHolder
            .GetOffers()
            .Where(o => o.User?.MemberType != MemberCategory.Trader)
            .Select(o => o.Id)
            .ToList();

        foreach (var id in toRemove)
            ragfairOfferHolder.RemoveOffer(id);

        if (_masterConfig.EnableDevLogs)
            logger.LogInformation("[RZCustomEconomy] Ragfair: {Count} dynamic offer(s) purged.", toRemove.Count);
    }
}
