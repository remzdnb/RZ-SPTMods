// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces
// ReSharper disable InvertIf

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

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

        if (!masterConfig.EnableRoutedTrades)
            return Task.CompletedTask;

        var autoRoutingConfig = configLoader.Load<RoutedTradesConfig>(RoutedTradesConfig.FileName);
        var traders = databaseService.GetTraders();
        var handbook = databaseService.GetTables().Templates?.Handbook;

        if (handbook is null)
        {
            logger.LogWarning("[RZCustomEconomy] Handbook is null -- skipping auto-routing.");
            return Task.CompletedTask;
        }

        // 1. Build blacklist.

        var blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!autoRoutingConfig.ForceRouteAll)
        {
            //if (autoRoutingConfig.UseStaticBlacklist)
                blacklist.UnionWith(masterConfig.StaticBlacklist);

            if (/*autoRoutingConfig.UseUserBlacklist && */masterConfig.UserBlacklist.ApplyToRoutedTrades)
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

        // 3. Build category route map : categoryId → (traderId, route).
        //    Dictionary is keyed by trader ID directly — no TraderIds.FromName needed.

        var catToParent = handbook.Categories.ToDictionary(
            c => c.Id.ToString(),
            c => c.ParentId?.ToString(),
            StringComparer.OrdinalIgnoreCase
        );

        var categoryToTraderRoute = BuildCategoryRouteMap(
            handbook.Categories,
            autoRoutingConfig.CategoryRoutes,
            autoRoutingConfig.ForceRouteAll
        );

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
                if (string.IsNullOrEmpty(ov.TraderId))
                {
                    skipped++;
                    continue;
                }

                if (!traders.TryGetValue(ov.TraderId, out var ovTrader))
                {
                    logger.LogWarning("[RZCustomEconomy] Override trader '{Id}' not found for '{Tpl}'.", ov.TraderId, itemTpl);
                    skipped++;
                    continue;
                }

                InjectAutoOffer(ovTrader.Assort, hbItem.Id, ov.PriceRoubles, ov.StackCount, ov.LoyaltyLevel, ov.BarterItems, 100);
                overridden++;
                continue;
            }

            // Normal route.

            if (!categoryToTraderRoute.TryGetValue(hbItem.ParentId, out var traderRoute))
            {
                if (autoRoutingConfig.ForceRouteAll && autoRoutingConfig.FallbackTraderId is not null)
                {
                    if (traders.TryGetValue(autoRoutingConfig.FallbackTraderId, out var fallbackTrader))
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

            var traderId = traderRoute.TraderId;
            var route = traderRoute.Route;

            if (!traders.TryGetValue(traderId, out var trader))
            {
                logger.LogWarning("[RZCustomEconomy] Route trader '{Id}' not found.", traderId);
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
    // Returns a map of categoryId → (traderId, route) with parent inheritance.
    // ─────────────────────────────────────────────────────────────────────────

    private static Dictionary<MongoId, (string TraderId, CategoryRoute Route)> BuildCategoryRouteMap(
        List<HandbookCategory> categories,
        Dictionary<string, List<CategoryRoute>> categoryRoutes,
        bool forceRouteAll
    )
    {
        // Flatten to categoryId → (traderId, route), respecting Enabled flag unless ForceRouteAll.
        var directRoutes = new Dictionary<string, (string TraderId, CategoryRoute Route)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (traderId, routes) in categoryRoutes)
        {
            foreach (var route in routes)
            {
                if (!forceRouteAll && !route.Enabled)
                    continue;

                // Last writer wins if a category is defined under multiple traders.
                directRoutes[route.CategoryId] = (traderId, route);
            }
        }

        var catById = categories.ToDictionary(c => c.Id.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = new Dictionary<MongoId, (string, CategoryRoute)>();

        foreach (var cat in categories)
        {
            var resolved = FindRouteForCategory(cat.Id.ToString(), catById, directRoutes);
            if (resolved.HasValue)
                result[cat.Id] = resolved.Value;
        }

        return result;
    }

    private static (string TraderId, CategoryRoute Route)? FindRouteForCategory(
        string categoryId,
        Dictionary<string, HandbookCategory> catById,
        Dictionary<string, (string TraderId, CategoryRoute Route)> directRoutes
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
