// RemzDNB - 2026
// ReSharper disable InvertIf
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

// should be -2
[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 2)]
public class AutoRoutingPatcher(
    ILogger<AutoRoutingPatcher> logger,
    DatabaseService databaseService,
    ConfigLoader configLoader,
    AssortHelper assortHelper
) : IOnLoad
{
    public Task OnLoad()
    {
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

        if (!masterConfig.EnableRoutedTrades) {
            return Task.CompletedTask;
        }

        var autoRoutingConfig = configLoader.Load<RoutedTradesConfig>(RoutedTradesConfig.FileName);
        var traders = databaseService.GetTraders();
        var handbook = databaseService.GetTables().Templates?.Handbook;

        if (handbook is null) {
            logger.LogWarning("[RZCustomEconomy] Handbook is null -- skipping auto-routing.");
            return Task.CompletedTask;
        }

        // 1. Build blacklist.

        var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!autoRoutingConfig.ForceRouteAll)
        {
            //if (autoRoutingConfig.UseStaticBlacklist)
                blacklist.UnionWith(masterConfig.StaticBlacklist);

            if (/*autoRoutingConfig.UseUserBlacklist &&*/ masterConfig.UserBlacklist.ApplyToRoutedTrades)
                blacklist.UnionWith(masterConfig.UserBlacklist.Items);
        }

        // 2. Build modded item set.

        if (autoRoutingConfig.RouteModdedItemsOnly && autoRoutingConfig.RouteVanillaItemsOnly)
        {
            logger.LogWarning(
                "[RZCustomEconomy] RouteModdedItemsOnly and RouteVanillaItemsOnly are both true -- "
                    + "mutually exclusive, both will be ignored."
            );
            autoRoutingConfig.RouteModdedItemsOnly = false;
            autoRoutingConfig.RouteVanillaItemsOnly = false;
        }

        HashSet<string>? moddedTpls = null;
        if (autoRoutingConfig.RouteModdedItemsOnly || autoRoutingConfig.RouteVanillaItemsOnly)
        {
            moddedTpls = BuildModdedItemSet(handbook);
            logger.LogInformation("[RZCustomEconomy] Detected {Count} modded item(s) via vanilla_handbook.json diff.", moddedTpls.Count);
        }

        // 3. Build category route map.

        var activeRoutes = autoRoutingConfig.ForceRouteAll ? autoRoutingConfig.CategoryRoutes : autoRoutingConfig.CategoryRoutes.Where(r => r.Enabled).ToList();
        var categoryToRoute = BuildCategoryRouteMap(handbook.Categories, activeRoutes);
        var overrides = autoRoutingConfig.Overrides.ToDictionary(o => o.ItemTpl, StringComparer.OrdinalIgnoreCase);

        int routed = 0, overridden = 0, skipped = 0;

        // 4. Route.

        foreach (var hbItem in handbook.Items)
        {
            var itemTpl = hbItem.Id.ToString();

            if (blacklist.Contains(itemTpl))
            {
                skipped++;
                continue;
            }

            if (moddedTpls is not null)
            {
                var isModded = moddedTpls.Contains(itemTpl);
                if (autoRoutingConfig.RouteModdedItemsOnly && !isModded || autoRoutingConfig.RouteVanillaItemsOnly && isModded)
                {
                    skipped++;
                    continue;
                }
            }

            // Override.

            if (autoRoutingConfig.EnableOverrides && overrides.TryGetValue(itemTpl, out var ov))
            {
                if (string.IsNullOrEmpty(ov.TraderName))
                {
                    skipped++;
                    continue;
                }

                var ovTraderId = TraderIds.FromName(ov.TraderName);
                if (ovTraderId is null || !traders.TryGetValue(ovTraderId, out var ovTrader))
                {
                    logger.LogWarning("[RZCustomEconomy] Override trader '{T}' not found for '{Tpl}'.", ov.TraderName, itemTpl);
                    skipped++;
                    continue;
                }

                InjectAutoOffer(ovTrader.Assort, hbItem.Id, ov.PriceRoubles, ov.StackCount, ov.LoyaltyLevel, ov.BarterItems, 100);
                overridden++;
                continue;
            }

            // Normal route.

            if (!categoryToRoute.TryGetValue(hbItem.ParentId, out var route))
            {
                if (autoRoutingConfig.ForceRouteAll && autoRoutingConfig.FallbackTrader is not null)
                {
                    var fallbackId = TraderIds.FromName(autoRoutingConfig.FallbackTrader);
                    if (fallbackId is not null && traders.TryGetValue(fallbackId, out var fallbackTrader))
                    {
                        var fallbackPrice = Math.Max(1, (int)Math.Round(hbItem.Price ?? 0));
                        InjectAutoOffer(fallbackTrader.Assort, hbItem.Id, fallbackPrice, -1, 1, new List<BarterItem>(), 100);
                        routed++;
                    }
                }
                else
                {
                    skipped++;
                }
                continue;
            }

            var traderId = TraderIds.FromName(route.TraderName);
            if (traderId is null || !traders.TryGetValue(traderId, out var trader))
            {
                logger.LogWarning("[RZCustomEconomy] Route trader '{T}' not found.", route.TraderName);
                skipped++;
                continue;
            }

            var handbookPrice = Math.Max(1, (int)Math.Round((hbItem.Price ?? 0) * route.PriceMultiplier));
            InjectAutoOffer(trader.Assort, hbItem.Id, handbookPrice, -1, route.LoyaltyLevel, new List<BarterItem>(), 100);
            routed++;
        }

        logger.LogInformation(
            "[RZCustomEconomy] {Routed} auto-routed, {Overridden} overridden, {Skipped} skipped.{RouteAll}",
            routed, overridden, skipped, autoRoutingConfig.ForceRouteAll ? " [ForceRouteAll ON]" : ""
        );

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InjectAutoOffer
    // ─────────────────────────────────────────────────────────────────────────

    private void InjectAutoOffer(
        TraderAssort assort,
        MongoId tpl,
        int priceRoubles,
        int stackCount,
        int loyaltyLevel,
        List<BarterItem> barterItems,
        int durability
    )
    {
        var itemId = new MongoId();

        assort.Items.Add(
            new Item
            {
                Id = itemId,
                Template = tpl,
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = stackCount <= 0,
                    StackObjectsCount = stackCount <= 0 ? 999999 : stackCount,
                    BuyRestrictionMax = null,
                    BuyRestrictionCurrent = null,
                },
            }
        );

        assortHelper.ResolveRequiredChildren(assort.Items, itemId, tpl, durability, new HashSet<string>());
        assort.BarterScheme[itemId] = new List<List<BarterScheme>> { assortHelper.BuildPayment(priceRoubles, barterItems) };
        assort.LoyalLevelItems[itemId] = loyaltyLevel;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildCategoryRouteMap
    // ─────────────────────────────────────────────────────────────────────────

    private static Dictionary<MongoId, CategoryRoute> BuildCategoryRouteMap(List<HandbookCategory> categories, List<CategoryRoute> routes)
    {
        var directRoutes = routes.ToDictionary(r => r.CategoryId, StringComparer.OrdinalIgnoreCase);
        var catById = categories.ToDictionary(c => c.Id.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<MongoId, CategoryRoute>();

        foreach (var cat in categories)
        {
            var route = FindRouteForCategory(cat.Id.ToString(), catById, directRoutes);
            if (route is not null)
                result[cat.Id] = route;
        }

        foreach (var route in routes)
        {
            MongoId key = route.CategoryId;
            if (!result.ContainsKey(key))
                result[key] = route;
        }

        return result;
    }

    private static CategoryRoute? FindRouteForCategory(
        string categoryId,
        Dictionary<string, HandbookCategory> catById,
        Dictionary<string, CategoryRoute> directRoutes
    )
    {
        var current = categoryId;
        while (current is not null)
        {
            if (directRoutes.TryGetValue(current, out var route))
                return route;
            if (!catById.TryGetValue(current, out var cat))
                break;
            current = cat.ParentId?.ToString();
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BuildModdedItemSet
    // ─────────────────────────────────────────────────────────────────────────

    private HashSet<string> BuildModdedItemSet(HandbookBase handbook)
    {
        var vanillaPath = System.IO.Path.Combine(AppContext.BaseDirectory, "user", "mods", "RZCustomEconomy", "vanilla_handbook.json");

        if (!System.IO.File.Exists(vanillaPath))
        {
            logger.LogWarning(
                "[RZCustomEconomy] vanilla_handbook.json not found at '{Path}'. "
                    + "RouteModdedItemsOnly / RouteVanillaItemsOnly will have no effect.",
                vanillaPath
            );
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        HashSet<string> vanillaTpls;
        try
        {
            var raw = System.IO.File.ReadAllText(vanillaPath);
            using var doc = System.Text.Json.JsonDocument.Parse(raw);

            if (!doc.RootElement.TryGetProperty("Items", out var itemsEl))
            {
                logger.LogWarning("[RZCustomEconomy] vanilla_handbook.json is empty or malformed -- modded item detection disabled.");
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            vanillaTpls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in itemsEl.EnumerateArray())
            {
                if (item.TryGetProperty("Id", out var idEl))
                    vanillaTpls.Add(idEl.GetString() ?? "");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("[RZCustomEconomy] Failed to parse vanilla_handbook.json: {Err}", ex.Message);
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return handbook
            .Items.Select(i => i.Id.ToString())
            .Where(tpl => !vanillaTpls.Contains(tpl))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
