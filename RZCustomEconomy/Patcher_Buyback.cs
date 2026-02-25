// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 1)]
public class BuybackPatcher(ILogger<BuybackPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);
        if (!masterConfig.EnableBuybackConfig)
            return Task.CompletedTask;

        var buybackConfig = configLoader.Load<BuybackConfig>(BuybackConfig.FileName);
        if (buybackConfig.Rules.Count == 0)
            return Task.CompletedTask;

        var traders = databaseService.GetTraders();
        var handbookTpls = databaseService.GetTables().Templates?.Handbook?.Items.Select(i => i.Id).ToHashSet();

        foreach (var (traderName, rule) in buybackConfig.Rules)
        {
            var traderId = TraderIds.FromName(traderName);
            if (traderId is null || !traders.TryGetValue(traderId, out var trader))
            {
                logger.LogWarning("[RZCustomEconomy] Buyback: trader '{Name}' not found -- skipping.", traderName);
                continue;
            }

            if (rule.Mode == BuybackMode.Default)
            {
                continue;
            }

            trader.Base.ItemsBuy = BuildItemBuyData(traderName, rule, handbookTpls);
        }

        return Task.CompletedTask;
    }

    private ItemBuyData BuildItemBuyData(string traderName, BuybackRule rule, HashSet<MongoId>? handbookTpls)
    {
        switch (rule.Mode)
        {
            case BuybackMode.Disabled:
                return new ItemBuyData { Category = new HashSet<MongoId>(), IdList = new HashSet<MongoId>() };

            case BuybackMode.Categories:
                var categories = rule.Categories.Select(c => new MongoId(c)).ToHashSet();
                return new ItemBuyData { Category = categories, IdList = new HashSet<MongoId>() };

            case BuybackMode.AllWithBlacklist:
                if (handbookTpls is null)
                {
                    logger.LogWarning("[RZCustomEconomy] {Trader}: handbook is null, cannot build buyback whitelist.", traderName);
                    return new ItemBuyData { Category = new HashSet<MongoId>(), IdList = new HashSet<MongoId>() };
                }

                var blacklist = rule.Blacklist.Select(t => new MongoId(t)).ToHashSet();
                var idList = handbookTpls.Where(tpl => !blacklist.Contains(tpl)).ToHashSet();
                return new ItemBuyData { Category = new HashSet<MongoId>(), IdList = idList };

            default:
                logger.LogWarning("[RZCustomEconomy] {Trader}: unknown buyback mode '{Mode}' -- disabling.", traderName, rule.Mode);
                return new ItemBuyData { Category = new HashSet<MongoId>(), IdList = new HashSet<MongoId>() };
        }
    }
}
