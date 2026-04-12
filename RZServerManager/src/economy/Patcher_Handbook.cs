// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.Economy;

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 4)]
public class HandbookPatcher(ILogger<HandbookPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    private readonly MasterConfig _masterConfig = configLoader.Load<MasterConfig>();
    private readonly HandbookPricesConfig _handbookPricesConfig = configLoader.Load<HandbookPricesConfig>();

    public Task OnLoad()
    {
        if (!_handbookPricesConfig.Enabled) {
            return Task.CompletedTask;
        }

        var config = configLoader.Load<HandbookPricesConfig>(HandbookPricesConfig.FileName);
        if (config.Prices.Count == 0) {
            return Task.CompletedTask;
        }

        var handbook = databaseService.GetTables().Templates?.Handbook;
        if (handbook is null) {
            logger.LogWarning("[RZCustomEconomy] Handbook is null : skipping price overrides.");
            return Task.CompletedTask;
        }

        var patched = 0;
        foreach (var (tpl, price) in config.Prices)
        {
            var entry = handbook.Items.FirstOrDefault(i => i.Id.ToString() == tpl);
            if (entry is null) {
                logger.LogWarning("[RZCustomEconomy] Handbook entry '{Tpl}' not found : skipping.", tpl);
                continue;
            }

            entry.Price = price;
            patched++;
        }

        if (_masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZCustomEconomy] {Count} handbook price(s) patched.", patched);
        }

        return Task.CompletedTask;
    }
}
