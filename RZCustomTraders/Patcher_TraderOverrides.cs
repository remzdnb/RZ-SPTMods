// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;

namespace RZCustomTraders;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 2)]
public class TraderOverridesPatcher(
    ILogger<TraderOverridesPatcher> logger,
    ModHelper                       modHelper,
    ImageRouter                     imageRouter,
    DatabaseService                 db,
    LocaleService                   localeService,
    ConfigLoader                    configLoader
) : IOnLoad
{
    public Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var modRoot = modHelper.GetAbsolutePathToModFolder(assembly);
        var config = configLoader.Load<MasterConfig>(MasterConfig.FileName, assembly);
        var forceIds = config.ForceApplyIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (config.EnableAvatarOverrides || forceIds.Count > 0) PatchAvatars(modRoot, config, forceIds);
        if (config.EnableLocaleOverrides || forceIds.Count > 0) PatchLocales(config, forceIds);
        if (config.EnableLoyaltyLevels || forceIds.Count > 0) PatchLoyaltyLevels(config, forceIds);
        if (config.EnableRepairOverrides || forceIds.Count > 0) PatchRepair(config, forceIds);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Avatars
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchAvatars(string modRoot, MasterConfig config, HashSet<string> forceIds)
    {
        foreach (var (traderId, fileName) in config.AvatarOverrides)
        {
            if (!config.EnableAvatarOverrides && !forceIds.Contains(traderId)) continue;

            var filePath = System.IO.Path.Combine(modRoot, "db", fileName);
            if (!File.Exists(filePath))
            {
                logger.LogWarning("[RZCustomTraders] TraderOverrides/Avatars: file not found, skipping: {File}", fileName);
                continue;
            }

            imageRouter.AddRoute($"/files/trader/avatar/{traderId}", filePath);
            //logger.LogInformation("[RZCustomTraders] TraderOverrides/Avatars: remapped '{Id}'.", traderId);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Locales
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchLocales(MasterConfig config, HashSet<string> forceIds)
    {
        var overrides = new Dictionary<string, string>();

        foreach (var (traderId, entry) in config.LocaleOverrides)
        {
            if (!config.EnableLocaleOverrides && !forceIds.Contains(traderId)) continue;

            if (!string.IsNullOrWhiteSpace(entry.FullName)) overrides[$"{traderId} FullName"] = entry.FullName;
            if (!string.IsNullOrWhiteSpace(entry.FirstName)) overrides[$"{traderId} FirstName"] = entry.FirstName;
            if (!string.IsNullOrWhiteSpace(entry.Nickname)) overrides[$"{traderId} Nickname"] = entry.Nickname;
            if (!string.IsNullOrWhiteSpace(entry.Location)) overrides[$"{traderId} Location"] = entry.Location;
            if (!string.IsNullOrWhiteSpace(entry.Description)) overrides[$"{traderId} Description"] = entry.Description;
        }

        if (overrides.Count == 0) return;

        foreach (var (_, locale) in db.GetTables().Locales.Global)
            locale.AddTransformer(dict =>
            {
                foreach (var (key, value) in overrides)
                    if (dict.ContainsKey(key))
                        dict[key] = value;
                return dict;
            });

        var en = localeService.GetLocaleDb("en");
        foreach (var (key, value) in overrides)
            if (en.ContainsKey(key))
                en[key] = value;

        //logger.LogInformation("[RZCustomTraders] TraderOverrides/Locales: {Count} trader(s) patched.", overrides.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LoyaltyLevels
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchLoyaltyLevels(MasterConfig config, HashSet<string> forceIds)
    {
        var traders = db.GetTables().Traders;

        foreach (var (traderId, levels) in config.LoyaltyLevelsOverrides)
        {
            if (!config.EnableLoyaltyLevels && !forceIds.Contains(traderId)) continue;

            if (!traders.TryGetValue(traderId, out var trader))
            {
                if (!forceIds.Contains(traderId))
                    logger.LogWarning("[RZCustomTraders] TraderOverrides/LoyaltyLevels: trader '{Id}' not found — skipping.", traderId);
                continue;
            }

            trader.Base.LoyaltyLevels = levels;
           // logger.LogInformation("[RZCustomTraders] TraderOverrides/LoyaltyLevels: '{Id}' — {Count} level(s) applied.", traderId, levels.Count);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Repair
    // ─────────────────────────────────────────────────────────────────────────

    private void PatchRepair(MasterConfig config, HashSet<string> forceIds)
    {
        var traders = db.GetTables().Traders;

        foreach (var (traderId, patch) in config.RepairOverrides)
        {
            if (!config.EnableRepairOverrides && !forceIds.Contains(traderId)) continue;

            if (!traders.TryGetValue(traderId, out var trader))
            {
                if (!forceIds.Contains(traderId))
                    logger.LogWarning("[RZCustomTraders] TraderOverrides/Repair: trader '{Id}' not found — skipping.", traderId);
                continue;
            }

            var repair = trader.Base.Repair;
            if (repair is null)
            {
                if (!forceIds.Contains(traderId))
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
}
