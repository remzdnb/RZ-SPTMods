// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.Economy;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class InsurancePatcher(ILogger<InsurancePatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig    = configLoader.Load<MasterConfig>();
        var insuranceConfig = configLoader.Load<InsuranceConfig>();

        var items = databaseService.GetTables().Templates?.Items;
        if (items is null)
        {
            logger.LogWarning("[RZSM] Templates.Items is null : skipping insurance patch.");
            return Task.CompletedTask;
        }

        // Disable all
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (insuranceConfig.DisableAll)
        {
            foreach (var (_, item) in items)
            {
                if (item.Properties is not null) {
                    item.Properties.InsuranceDisabled = true;
                }
            }

            if (masterConfig.EnableDevLogs)
                logger.LogInformation("[RZSM] Insurance disabled on all items.");

            return Task.CompletedTask;
        }

        // Category + TPL blacklist
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        var enabledCategories = insuranceConfig.CategoryBlacklist
            .Where(e => e.Enabled)
            .Select(e => e.CategoryId)
            .ToList();

        var blacklistedTpls = ItemUtils.BuildCategoryTplSet(enabledCategories, items);

        foreach (var tpl in insuranceConfig.TplBlacklist) {
            blacklistedTpls.Add(tpl);
        }

        if (blacklistedTpls.Count == 0) {
            return Task.CompletedTask;
        }

        foreach (var (tpl, item) in items)
        {
            if (item.Properties is not null && blacklistedTpls.Contains(tpl.ToString())) {
                item.Properties.InsuranceDisabled = true;
            }
        }

        if (masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZSM] Insurance disabled on {Count} item(s).", blacklistedTpls.Count);
        }

        return Task.CompletedTask;
    }
}
