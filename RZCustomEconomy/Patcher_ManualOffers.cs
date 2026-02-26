// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 2)]
public class ManualOffersPatcher(
    ILogger<ManualOffersPatcher> logger,
    DatabaseService databaseService,
    ConfigLoader configLoader,
    AssortHelper assortHelper
) : IOnLoad
{
    public Task OnLoad()
    {
        var userConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);

        if (!userConfig.EnableManualOffersConfig)
            return Task.CompletedTask;

        var config = configLoader.Load<ManualOffersConfig>(ManualOffersConfig.FileName);

        if (config.ManualOffers.Count == 0)
            return Task.CompletedTask;

        var traders = databaseService.GetTraders();
        var manualById = config.ManualOffers.ToDictionary(t => t.Id, StringComparer.OrdinalIgnoreCase);

        var injected = 0;
        foreach (var (id, trader) in traders)
        {
            if (!manualById.TryGetValue(id.ToString(), out var manualOffers))
                continue;

            InjectManualOffers(trader.Assort, manualOffers.Offers);
            injected += manualOffers.Offers.Count;
        }

        logger.LogInformation("[RZCustomEconomy] {Count} manual offer(s) injected.", injected);

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
                    Template = offer.ItemTpl,
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
                        Template = child.ItemTpl,
                        ParentId = itemId,
                        SlotId = child.SlotId,
                        Upd = new Upd { StackObjectsCount = child.Count },
                    }
                );
            }

            var manualSlots = offer.Children.Select(c => c.SlotId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            assortHelper.ResolveRequiredChildren(assort.Items, itemId, offer.ItemTpl, offer.Durability, manualSlots);

            assort.BarterScheme[itemId] = new List<List<BarterScheme>> { assortHelper.BuildPayment(offer.PriceRoubles, offer.BarterItems) };
            assort.LoyalLevelItems[itemId] = offer.LoyaltyLevel;
        }
    }
}
