// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.UI;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1000)]
public class LocalePatcher(
    ILogger<LocalePatcher> logger,
    DatabaseService databaseService,
    LocaleService localeService,
    ConfigLoader configLoader
) : IOnLoad
{
    private static readonly string[] ItemSuffixes = [" Name", " ShortName", " Description"];

    public Task OnLoad()
    {
        var config = configLoader.Load<UIConfig>();

        var hasItemMaps = config.ForceEnglishItems || config.ForceEnglishMaps;
        var hasHideout = config.EnableHideoutLocaleOverrides && config.HideoutLocaleOverrides.Count > 0;
        var hasTraderLocales = config.EnableTraderLocaleOverrides && config.TraderLocaleOverrides.Count > 0;

        if (!hasItemMaps && !hasHideout && !hasTraderLocales)
            return Task.CompletedTask;

        var locationTpls = databaseService.GetTables().Locations?
            .GetDictionary()
            .Values
            .Where(loc => loc?.Base?.IdField != null)
            .Select(loc => loc!.Base!.IdField.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var english = localeService.GetLocaleDb("en");

        // Build trader overrides dict once
        var traderOverrides = new Dictionary<string, string>();
        if (hasTraderLocales)
        {
            foreach (var (traderId, entry) in config.TraderLocaleOverrides)
            {
                if (!string.IsNullOrWhiteSpace(entry.FullName)) traderOverrides[$"{traderId} FullName"] = entry.FullName;
                if (!string.IsNullOrWhiteSpace(entry.FirstName)) traderOverrides[$"{traderId} FirstName"] = entry.FirstName;
                if (!string.IsNullOrWhiteSpace(entry.Nickname)) traderOverrides[$"{traderId} Nickname"] = entry.Nickname;
                if (!string.IsNullOrWhiteSpace(entry.Location)) traderOverrides[$"{traderId} Location"] = entry.Location;
                if (!string.IsNullOrWhiteSpace(entry.Description)) traderOverrides[$"{traderId} Description"] = entry.Description;
            }
        }

        // Attach transformer to every non-English language
        foreach (var (langCode, lazyLoad) in databaseService.GetLocales().Global)
        {
            if (string.Equals(langCode, "en", StringComparison.OrdinalIgnoreCase))
                continue;

            lazyLoad.AddTransformer(dict =>
            {
                if (dict is null) return dict;

                if (hasItemMaps)
                {
                    foreach (var kvp in english)
                    {
                        if (!ShouldOverride(kvp.Key, config, locationTpls)) continue;
                        if (!dict.ContainsKey(kvp.Key)) continue;
                        dict[kvp.Key] = kvp.Value;
                    }
                }

                if (hasHideout)
                {
                    foreach (var (key, value) in config.HideoutLocaleOverrides)
                        if (!string.IsNullOrWhiteSpace(value))
                            dict[key] = value;
                }

                if (hasTraderLocales)
                {
                    foreach (var (key, value) in traderOverrides)
                        if (dict.ContainsKey(key))
                            dict[key] = value;
                }

                return dict;
            });
        }

        // Patch English directly
        if (hasHideout)
        {
            foreach (var (key, value) in config.HideoutLocaleOverrides)
                if (!string.IsNullOrWhiteSpace(value))
                    english[key] = value;
        }

        if (hasTraderLocales)
        {
            foreach (var (key, value) in traderOverrides)
                if (english.ContainsKey(key))
                    english[key] = value;
        }

        logger.LogInformation("[RZSM] LocalePatcher: transformers attached to {Count} language(s).",
            databaseService.GetLocales().Global.Count - 1);

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static bool ShouldOverride(string key, UIConfig config, HashSet<string> locationTpls)
    {
        if (IsMapKey(key, locationTpls))
            return config.ForceEnglishMaps;

        if (IsItemKey(key))
            return config.ForceEnglishItems;

        return false;
    }

    private static bool IsItemKey(string key)
    {
        foreach (var suffix in ItemSuffixes)
        {
            if (key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static bool IsMapKey(string key, HashSet<string> locationTpls)
    {
        foreach (var suffix in ItemSuffixes)
        {
            if (!key.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                continue;

            var prefix = key[..^suffix.Length];
            if (locationTpls.Contains(prefix))
                return true;
        }
        return false;
    }
}
