// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using RZServerManager._Shared;

namespace RZServerManager.Traders;

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 2)]
public class Patcher_Trades_Manual(
    ILogger<Patcher_Trades_Manual> logger,
    DatabaseService databaseService,
    ConfigLoader configLoader,
    AssortUtilities assortUtilities
) : IOnLoad
{
    public Task OnLoad()
    {
        var manualTradesConfig = configLoader.Load<ManualTradesConfig>();
        if (manualTradesConfig.ManualOffers.Count == 0)
            return Task.CompletedTask;

        var masterConfig = configLoader.Load<MasterConfig>();
        var traders = databaseService.GetTraders();
        var manualById = manualTradesConfig.ManualOffers.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        var injected = 0;
        foreach (var (id, trader) in traders)
        {
            if (!manualById.TryGetValue(id.ToString(), out var manualOffers))
                continue;

            var validOffers = manualOffers.Offers.Where(o => ValidateOffer(o, id.ToString())).ToList();
            InjectManualOffers(trader.Assort, validOffers);
            injected += validOffers.Count;
        }

        if (masterConfig.EnableDevLogs) {
            logger.LogInformation("[RZSM] {Count} manual offer(s) injected.", injected);
        }

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InjectManualOffers
    // ─────────────────────────────────────────────────────────────────────────

    private void InjectManualOffers(TraderAssort assort, List<TradeOffer> offers)
    {
        foreach (var offer in offers)
        {
            var itemId = new MongoId();

            assort.Items.Add(
                new Item
                {
                    Id = itemId,
                    Template = offer.Tpl,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = new Upd
                    {
                        UnlimitedCount = offer.StackCount <= 0,
                        StackObjectsCount = offer.StackCount <= 0 ? 999999 : offer.StackCount,
                        BuyRestrictionMax = null,
                        BuyRestrictionCurrent = null,
                        Repairable = offer.Durability is > 0 and < 100
                            ? new UpdRepairable { MaxDurability = 100, Durability = offer.Durability }
                            : null,
                    },
                }
            );

            foreach (var child in offer.Children)
            {
                assort.Items.Add(
                    new Item
                    {
                        Id = new MongoId(),
                        Template = child.Tpl,
                        ParentId = itemId,
                        SlotId = child.SlotId,
                        Upd = new Upd { StackObjectsCount = child.Count },
                    }
                );
            }

            var manualSlots = offer.Children.Select(c => c.SlotId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            assortUtilities.ResolveRequiredChildren(assort.Items, itemId, offer.Tpl, offer.Durability, manualSlots);

            var barterItems = offer.BarterItems ?? [];
            var price = offer.PriceRoubles;
            if (price <= 0 && barterItems.Count == 0)
            {
                var handbook = databaseService.GetTables().Templates?.Handbook;
                price = (int) (handbook?.Items.FirstOrDefault(h => h.Id == offer.Tpl)?.Price ?? 0);
                if (price <= 0)
                    logger.LogWarning("[RZCustomEconomy] Manual offer {Tpl}: no price and no handbook price found, defaulting to 1.",
                        offer.Tpl);
            }

            assort.BarterScheme[itemId] = new List<List<BarterScheme>>
            {
                assortUtilities.BuildPayment(price, barterItems)
            };
            assort.LoyalLevelItems[itemId] = offer.LoyaltyLevel;
        }
    }

    private bool ValidateOffer(TradeOffer offer, string traderId)
    {
        if (string.IsNullOrWhiteSpace(offer.Tpl))
        {
            logger.LogError("[RZSM] Manual offer with empty ItemTpl for trader '{Id}' : skipping.", traderId);
            return false;
        }

        var emptyBarters = offer.BarterItems.Where(b => string.IsNullOrWhiteSpace(b.Tpl)).ToList();
        if (emptyBarters.Count > 0)
        {
            logger.LogError("[RZSM] Manual offer '{Tpl}' for trader '{Id}' has {Count} barter item(s) with empty Tpl : skipping.",
                offer.Tpl, traderId, emptyBarters.Count);
            return false;
        }

        var emptyChildren = offer.Children.Where(c => string.IsNullOrWhiteSpace(c.Tpl)).ToList();
        if (emptyChildren.Count > 0)
        {
            logger.LogError("[RZSM] Manual offer '{Tpl}' for trader '{Id}' has {Count} child(ren) with empty Tpl : skipping.",
                offer.Tpl, traderId, emptyChildren.Count);
            return false;
        }

        return true;
    }
}
