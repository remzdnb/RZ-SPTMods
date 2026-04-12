// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.Traders;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 2)]
public class Patcher_TraderMiscSettings(
    ILogger<Patcher_TraderMiscSettings> logger,
    DatabaseService db,
    ConfigLoader configLoader
) : IOnLoad
{
    private readonly TraderMiscSettingsConfig _traderTraderMiscSettingsConfig = configLoader.Load<TraderMiscSettingsConfig>();

    public Task OnLoad()
    {
        PatchLoyaltyLevels();
        PatchRepair();

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // LOYALTY LEVELS
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void PatchLoyaltyLevels()
    {
        if (!_traderTraderMiscSettingsConfig.EnableLoyaltyLevels)
            return;

        var traders = db.GetTables().Traders;

        foreach (var (traderId, levels) in _traderTraderMiscSettingsConfig.LoyaltyLevelsOverrides)
        {
            if (!traders.TryGetValue(traderId, out var trader)) {
                continue;
            }

            trader.Base.LoyaltyLevels = levels;
           // logger.LogInformation("[RZCustomTraders] TraderOverrides/LoyaltyLevels: '{Id}' — {Count} level(s) applied.", traderId, levels.Count);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // REPAIRS
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private void PatchRepair()
    {
        if (!_traderTraderMiscSettingsConfig.EnableRepairOverrides)
            return;

        var traders = db.GetTables().Traders;

        foreach (var (traderId, patch) in _traderTraderMiscSettingsConfig.RepairOverrides)
        {
            if (!traders.TryGetValue(traderId, out var trader)) {
                continue;
            }

            var repair = trader.Base.Repair;
            if (repair is null)
            {
                logger.LogWarning("[RZCustomTraders] TraderOverrides/Repair: trader '{Id}' has no repair block — skipping.", traderId);
                continue;
            }

            if (patch.Availability is not null) repair.Availability = patch.Availability;
            if (patch.Currency is not null) repair.Currency = patch.Currency;
            if (patch.CurrencyCoefficient is not null) repair.CurrencyCoefficient = patch.CurrencyCoefficient;
            if (patch.Quality is not null) repair.Quality = patch.Quality;
            if (patch.PriceRate is not null) repair.PriceRate = patch.PriceRate;
            if (patch.ExcludedIdList is not null) repair.ExcludedIdList = patch.ExcludedIdList;
            if (patch.ExcludedCategory is not null) repair.ExcludedCategory = patch.ExcludedCategory;

           // logger.LogInformation("[RZCustomTraders] TraderOverrides/Repair: '{Id}' patched.", traderId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // MEDIC
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    // ToDo : free medic
}
