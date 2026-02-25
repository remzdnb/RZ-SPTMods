// RemzDNB - 2026
// ReSharper disable EnforceIfStatementBraces
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace RZCustomEconomy;

[Injectable]
public class AssortHelper(ILogger<AssortHelper> logger, DatabaseService databaseService, ConfigLoader configLoader)
{
    // Backing field - holds the cached value after first load.
    private Dictionary<MongoId, TemplateItem>? _itemTemplates;

    // Lazy accessor - loads once on first access, returns cached value afterwards.
    private Dictionary<MongoId, TemplateItem> ItemTemplates
    {
        get { return _itemTemplates ??= databaseService.GetTables().Templates?.Items!; }
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // BuildPayment
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    public List<BarterScheme> BuildPayment(int priceRoubles, List<BarterItem> barterItems)
    {
        var config = configLoader.Load<MasterConfig>(MasterConfig.FileName);

        if (config.EnableDevMode)
        {
            // In DevMode, all prices are forced to 1 rouble regardless of config.
            return [new BarterScheme { Template = ItemTpl.MONEY_ROUBLES, Count = 1 }];
        }

        var payment = new List<BarterScheme>();

        if (priceRoubles > 0)
        {
            // Add roubles.
            payment.Add(new BarterScheme { Template = ItemTpl.MONEY_ROUBLES, Count = priceRoubles });
        }

        foreach (var b in barterItems)
        {
            // Add barter items.
            payment.Add(new BarterScheme { Template = b.ItemTpl, Count = b.Count });
        }

        return payment.Count > 0 ? payment : [new BarterScheme { Template = ItemTpl.MONEY_ROUBLES, Count = 0 }];
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // ResolveRequiredChildren
    // Recursively injects required child items (armor plates, etc.) by reading the Slots of the TemplateItem from the DB.
    // ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

    public void ResolveRequiredChildren(
        List<Item> items,
        MongoId parentId,
        MongoId parentTpl,
        int durability,
        HashSet<string> skipSlots,
        int depth = 0
    )
    {
        if (depth > 5)
            return;

        if (!ItemTemplates.TryGetValue(parentTpl, out var template))
            return;

        var slots = template.Properties?.Slots;
        if (slots is null)
            return;

        foreach (var slot in slots)
        {
            if (slot.Required != true || slot.Name is null)
                continue;

            if (skipSlots.Contains(slot.Name))
                continue;

            var childTpl = ResolveSlotDefaultTpl(slot);
            if (childTpl is null)
                continue;

            var childId = new MongoId();

            UpdRepairable? repairable = null;
            if (ItemTemplates.TryGetValue(childTpl.Value, out var childTemplate))
            {
                var maxDur = (int?)childTemplate.Properties?.Durability;
                if (maxDur is > 0)
                {
                    var ratio = durability <= 0 ? 1.0 : Math.Clamp(durability / 100.0, 0.0, 1.0);
                    repairable = new UpdRepairable { MaxDurability = maxDur.Value, Durability = (int)Math.Round(maxDur.Value * ratio) };
                }
            }

            items.Add(new Item
            {
                Id = childId,
                Template = childTpl.Value,
                ParentId = parentId,
                SlotId = slot.Name,
                Upd = repairable is not null ? new Upd { Repairable = repairable } : null,
            });

            ResolveRequiredChildren(items, childId, childTpl.Value, durability, new HashSet<string>(), depth + 1);
        }
    }

    private static MongoId? ResolveSlotDefaultTpl(Slot slot)
    {
        var filters = slot.Properties?.Filters;
        if (filters is null)
            return null;

        foreach (var filter in filters)
        {
            if (filter.Plate is not null && filter.Plate.Value != default)
                return filter.Plate.Value;

            if (filter.Filter is not null && filter.Filter.Count > 0)
                return filter.Filter.First();
        }

        return null;
    }
}
