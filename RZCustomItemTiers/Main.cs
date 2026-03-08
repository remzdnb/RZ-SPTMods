// RemzDNB 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomItemTiers;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 10)]
public class ColorPatcher(ILogger<ColorPatcher> logger, DatabaseService databaseService, ConfigLoader configLoader) : IOnLoad
{
    public Task OnLoad()
    {
        var items = databaseService.GetTables().Templates?.Items;
        if (items is null)
        {
            logger.LogError("[RZCustomItemTiers] Templates.Items is null — aborting.");
            return Task.CompletedTask;
        }

        var handbook = databaseService.GetTables().Templates?.Handbook;

        // Load configs
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, Assembly.GetExecutingAssembly());
        if (masterConfig?.Tiers is null || masterConfig.Tiers.Count == 0)
        {
            logger.LogError("[RZCustomItemTiers] masterConfig.json is empty or missing — aborting.");
            return Task.CompletedTask;
        }

        var categoryRules = configLoader.Load<CategoryRulesConfig>(CategoryRulesConfig.FileName, Assembly.GetExecutingAssembly());
        var priceRules    = configLoader.Load<PriceRulesConfig>(PriceRulesConfig.FileName, Assembly.GetExecutingAssembly());
        var tplOverrides  = LoadAndMergeOverrides();

        // Pre-sort price thresholds descending so first match wins
        masterConfig.PriceThresholds.Sort((a, b) => b.MinPrice.CompareTo(a.MinPrice));

        // Build handbook price lookup
        var handbookPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (handbook?.Items is not null)
        {
            foreach (var entry in handbook.Items) {
                handbookPrices[entry.Id.ToString()] = (int)(entry.Price ?? 0);
            }
        }

        var defaultColor = masterConfig.Tiers.GetValueOrDefault("Default", "default");

        // Build price rule category set for fast lookup (same DB IDs as categoryRules)
        var priceRuleCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (priceRules?.Enabled == true)
        {
            foreach (var cat in priceRules.Categories) {
                priceRuleCategories.Add(cat);
            }
        }

        int pass1 = 0, pass2 = 0, pass3 = 0;

        // Pass 1 - Category assignments
        // Walk DB parent chain to find most specific category match. Every item gets at least Default.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var (mongoId, item) in items)
        {
            if (item.Properties is null) continue;

            var tier = categoryRules?.Assignments is not null
                ? FindCategoryTier(mongoId.ToString(), items, categoryRules.Assignments)
                : null;

            item.Properties.BackgroundColor = tier is not null && masterConfig.Tiers.TryGetValue(tier, out var c) ? c : defaultColor;
            pass1++;
        }

        // Pass 2 - Price-based tiering
        // Walk DB parent chain to find if item belongs to a priceRules category, then resolve tier from handbook
        // price + masterConfig PriceThresholds.
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        if (priceRuleCategories.Count > 0 && masterConfig.PriceThresholds.Count > 0)
        {
            foreach (var (mongoId, item) in items)
            {
                if (item.Properties is null) continue;

                var tpl = mongoId.ToString();
                if (!handbookPrices.TryGetValue(tpl, out var price) || price <= 0) continue;

                var categoryId = FindMatchingCategory(tpl, items, priceRuleCategories);
                if (categoryId is null) continue;

                var tier = ResolvePriceTier(price, masterConfig.PriceThresholds) ?? "Default";

                if (!masterConfig.Tiers.TryGetValue(tier, out var color)) continue;

                item.Properties.BackgroundColor = color;
                pass2++;
            }
        }

        // Pass 3 - Per-TPL overrides (absolute priority)
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var (tpl, tierName) in tplOverrides)
        {
            if (!items.TryGetValue(tpl, out var item))
            {
                logger.LogWarning("[RZCustomItemTiers] Override TPL '{Tpl}' not found in DB — skipped.", tpl);
                continue;
            }

            if (item.Properties is null) continue;

            if (!masterConfig.Tiers.TryGetValue(tierName, out var color))
            {
                logger.LogWarning("[RZCustomItemTiers] Override TPL '{Tpl}': unknown tier '{Tier}' — skipped.", tpl, tierName);
                continue;
            }

            item.Properties.BackgroundColor = color;
            pass3++;
        }

        // Pass 4 - Ammo boxes inherit tier from their cartridge
        // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

        foreach (var (mongoId, item) in items)
        {
            if (item.Properties?.StackSlots is null) continue;

            var cartridgeTpl = item.Properties.StackSlots
                .FirstOrDefault(s => s.Name == "cartridges")?
                .Properties?.Filters?
                .FirstOrDefault()?
                .Filter?
                .FirstOrDefault()
                .ToString();

            if (cartridgeTpl is null) continue;
            if (!items.TryGetValue(cartridgeTpl, out var cartridge)) continue;
            if (cartridge.Properties?.BackgroundColor is null) continue;

            item.Properties.BackgroundColor = cartridge.Properties.BackgroundColor;
        }

        //

        logger.LogInformation(
            "[RZCustomItemTiers] Done — {P1} categorized, {P2} price-tiered, {P3} TPL override(s).",
            pass1, pass2, pass3);

        return Task.CompletedTask;
    }

    // Resolve price against sorted (desc) threshold list
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static string? ResolvePriceTier(int price, List<PriceThreshold> thresholds)
    {
        foreach (var threshold in thresholds)
        {
            if (price >= threshold.MinPrice)
                return threshold.Tier;
        }
        return null;
    }

    // Walk DB parent chain, return first matching category tier
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static string? FindCategoryTier(
        string tpl,
        Dictionary<MongoId, TemplateItem> items,
        Dictionary<string, string> assignments)
    {
        if (!items.TryGetValue(tpl, out var item))
            return null;

        var current = item.Parent.ToString();

        while (!string.IsNullOrEmpty(current))
        {
            if (assignments.TryGetValue(current, out var tier))
                return tier;

            if (!items.TryGetValue(current, out var parent))
                break;

            current = parent.Parent.ToString();
        }

        return null;
    }

    // Walk DB parent chain, return first category ID present in the set
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private static string? FindMatchingCategory(
        string tpl,
        Dictionary<MongoId, TemplateItem> items,
        HashSet<string> categories)
    {
        if (!items.TryGetValue(tpl, out var item))
            return null;

        var current = item.Parent.ToString();

        while (!string.IsNullOrEmpty(current))
        {
            if (categories.Contains(current))
                return current;

            if (!items.TryGetValue(current, out var parent))
                break;

            current = parent.Parent.ToString();
        }

        return null;
    }

    // Merge all *.json from config/ (excluding reserved files)
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    private Dictionary<string, string> LoadAndMergeOverrides()
    {
        var merged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var files = configLoader.LoadAll<OverrideFile>(
            "config",
            Assembly.GetExecutingAssembly(),
            exclude: [MasterConfig.FileName, CategoryRulesConfig.FileName, PriceRulesConfig.FileName]
        );

        foreach (var file in files)
        {
            if (!file.Enabled) continue;

            foreach (var (tpl, tier) in file.Overrides)
            {
                if (merged.ContainsKey(tpl))
                    logger.LogWarning("[RZCustomItemTiers] Duplicate TPL override '{Tpl}' — last file wins.", tpl);

                merged[tpl] = tier;
            }
        }

        return merged;
    }
}
