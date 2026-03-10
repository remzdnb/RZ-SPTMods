// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class InsurancePatcher(ILogger<InsurancePatcher> logger, DatabaseService databaseService, ConfigLoader configLoader ) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());
        if (!masterConfig.EnableInsuranceConfig)
            return Task.CompletedTask;

        var insuranceConfig = configLoader.Load<InsuranceConfig>(InsuranceConfig.FileName, Assembly.GetExecutingAssembly());

        var items = databaseService.GetTables().Templates?.Items;
        if (items is null) {
            logger.LogWarning("[RZCustomEconomy] Templates.Items is null : skipping insurance patch.");
            return Task.CompletedTask;
        }

        if (insuranceConfig.DisableAll)
        {
            foreach (var (_, item) in items)
            {
                if (item.Properties is not null)
                    item.Properties.InsuranceDisabled = true;
            }

            if (masterConfig.EnableDevLogs) {
                logger.LogInformation("[RZCustomEconomy] Insurance disabled on all items.");
            }

            return Task.CompletedTask;
        }

        var blacklistedTpls = BuildTplSet(insuranceConfig, items);
        if (blacklistedTpls.Count == 0)
            return Task.CompletedTask;

        foreach (var (tpl, item) in items)
        {
            if (item.Properties is not null && blacklistedTpls.Contains(tpl.ToString()))
                item.Properties.InsuranceDisabled = true;
        }

        if (masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZCustomEconomy] Insurance disabled on {Count} item(s).", blacklistedTpls.Count);
        }

        return Task.CompletedTask;
    }

    private HashSet<string> BuildTplSet(InsuranceConfig config, Dictionary<MongoId, TemplateItem> items)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var tpl in config.TplBlacklist) {
            result.Add(tpl);
        }

        var enabledCategories = config.CategoryBlacklist.Where(e => e.Enabled).ToList();
        if (enabledCategories.Count > 0)
        {
            var handbook = databaseService.GetTables().Templates?.Handbook;
            if (handbook is null)
            {
                logger.LogWarning("[RZCustomEconomy] Handbook is null — category blacklist skipped.");
            }
            else
            {
                var resolvedCategories = BuildCategorySet(enabledCategories.Select(e => e.CategoryId).ToList(), handbook);
                foreach (var handbookItem in handbook.Items)
                {
                    if (resolvedCategories.Contains(handbookItem.ParentId.ToString()))
                        result.Add(handbookItem.Id.ToString());
                }
            }
        }

        return result;
    }

    private static HashSet<string> BuildCategorySet(List<string> roots, HandbookBase handbook)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>(roots);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!result.Add(current))
                continue;

            foreach (var child in handbook.Categories.Where(c => c.ParentId?.ToString() == current)) {
                queue.Enqueue(child.Id.ToString());
            }
        }

        return result;
    }
}
