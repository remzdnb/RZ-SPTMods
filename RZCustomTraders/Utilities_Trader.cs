// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;

namespace RZCustomTraders;

[Injectable]
public class TraderService(
    ILogger<TraderService> logger,
    DatabaseService        db,
    ImageRouter            imageRouter,
    ICloner                cloner
)
{
    public void Register(TraderBase traderBase, string imagePath)
    {
        RouteImage(traderBase, imagePath);
        RegisterInDatabase(traderBase);
        RegisterLocales(traderBase);

        //logger.LogInformation("[RZCustomTraders] Trader '{Name}' registered (id: {Id}).", traderBase.Name, traderBase.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void RouteImage(TraderBase traderBase, string imagePath)
    {
        var routeKey = traderBase.Avatar.Replace(".png", "").Replace(".jpg", "");
        imageRouter.AddRoute(routeKey, imagePath);
    }

    private void RegisterInDatabase(TraderBase traderBase)
    {
        var emptyAssort = new TraderAssort
        {
            Items           = [],
            BarterScheme    = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };

        var traderEntry = new Trader
        {
            Base        = cloner.Clone(traderBase),
            Assort      = emptyAssort,
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() },
            },
            Dialogue = new Dictionary<string, List<string>?>()
        };

        if (!db.GetTables().Traders.TryAdd(traderBase.Id, traderEntry))
            logger.LogWarning("[RZCustomTraders] Trader '{Id}' already exists in DB, skipping.", traderBase.Id);
    }

    private void RegisterLocales(TraderBase traderBase)
    {
        var id = traderBase.Id.ToString();

        // Seed empty locale keys so TraderOverridesPatcher can overwrite them via dict[key] = value.
        foreach (var (_, locale) in db.GetTables().Locales.Global)
        {
            locale.AddTransformer(data =>
            {
                data.TryAdd($"{id} FullName", "");
                data.TryAdd($"{id} FirstName", "");
                data.TryAdd($"{id} Nickname", "");
                data.TryAdd($"{id} Location", "");
                data.TryAdd($"{id} Description", "");
                return data;
            });
        }
    }
}
