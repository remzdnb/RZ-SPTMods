// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace RZCustomProfiles;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader)]
public class ProfilesPatcher(
    ILogger<ProfilesPatcher> logger,
    DatabaseService databaseService,
    LocaleService localeService,
    ConfigLoader configLoader,
    ProfilesUtilities profilesUtilities
) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());
        var profileConfigs = configLoader.LoadAll<ProfileConfig>("profiles", Assembly.GetExecutingAssembly()).ToList();
        var sptProfiles = databaseService.GetProfileTemplates();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Sanity checks
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        var duplicates = profileConfigs
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in duplicates)
        {
            logger.LogError(
                "[RZCustomProfiles] Profile name '{Name}' is defined {Count} times : profiles will overwrite each other. Check your profiles/ folder.", group.Key, group.Count()
            );
        }

        // Unlock Jaeger and Ref
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (masterConfig.UnlockJaeger)
        {
            var traders = databaseService.GetTraders();
            if (traders.TryGetValue(Traders.JAEGER, out var jaegerTrader)) {
                jaegerTrader.Base.UnlockedByDefault = true;
            }
        }

        if (masterConfig.UnlockRef)
        {
            var traders = databaseService.GetTraders();
            if (traders.TryGetValue(Traders.REF, out var refTrader)) {
                refTrader.Base.UnlockedByDefault = true;
            }
        }

        // Register custom profiles.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var config in profileConfigs)
        {
            if (!config.Enabled) {
                continue;
            }

            if (!MasterConfig.BaseProfiles.TryGetValue(config.BaseProfile, out var baseProfileName))
            {
                logger.LogWarning("[RZCustomProfiles] Invalid BaseProfile '{Id}' for '{Name}' — skipping.", config.BaseProfile, config.Name);
                continue;
            }

            if (!sptProfiles.TryGetValue(baseProfileName, out var baseProfile))
            {
                logger.LogWarning("[RZCustomProfiles] Base profile '{Base}' not found for '{Name}' — skipping.", baseProfileName, config.Name);
                continue;
            }

            var cloned = FastCloner.FastCloner.DeepClone(baseProfile);
            if (cloned is null)
            {
                logger.LogWarning("[RZCustomProfiles] Failed to clone '{Base}' for profile '{Name}' — skipping.", baseProfileName, config.Name);
                continue;
            }

            foreach (var locale in databaseService.GetTables().Locales.Languages.Keys) {
                localeService.GetLocaleDb(locale).TryAdd($"launcher-profile_{config.Name}", config.Description ?? config.Name);
            }

            cloned.DescriptionLocaleKey = config.Description ?? config.Name;

            profilesUtilities.PatchSide(cloned.Usec, "USEC", config);
            profilesUtilities.PatchSide(cloned.Bear, "BEAR", config);

            sptProfiles[config.Name] = cloned;
            allowed.Add(config.Name);
        }

        // Add enabled default profiles.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var index in masterConfig.EnabledBaseProfiles)
        {
            if (MasterConfig.BaseProfiles.TryGetValue(index, out var profileName)) {
                allowed.Add(profileName);
            }
            else {
                logger.LogWarning("[RZCustomProfiles] Unknown SPT profile index '{Index}' — skipped.", index);
            }
        }

        // Delete non allowed profiles.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var key in sptProfiles.Keys.Where(k => !allowed.Contains(k)).ToList()) {
            sptProfiles.Remove(key);
        }

        return Task.CompletedTask;
    }
}
