// RemzDNB - 2026

using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;
using RZServerManager._Shared;
using SPTarkov.Server.Core.Helpers;

namespace RZServerManager.Traders;

// ─────────────────────────────────────────────────────────────────────────────
// CUSTOM TRADER PATCHER  (PostDBModLoader + 1)
//
// Registers Sable as a fully autonomous trader entry in the database.
// Must be complete and self-contained at registration time — no second-pass patchers.
// ─────────────────────────────────────────────────────────────────────────────

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class CustomTraderPatcher(
    ILogger<CustomTraderPatcher> logger,
    DatabaseService              databaseService,
    ConfigLoader                 configLoader,
    ModHelper                    modHelper,
    ImageRouter                  imageRouter,
    LocaleService                localeService,
    ConfigServer                 configServer,
    ICloner                      cloner
) : IOnLoad
{
   // private const string SableId = "fa363fc015e07b5f7828c9f7";

    public Task OnLoad()
    {
        var config  = configLoader.Load<CustomTraderConfig>();
        var modRoot = modHelper.GetAbsolutePathToModFolder(System.Reflection.Assembly.GetExecutingAssembly());

        if (!config.EnableCustomTrader)
            return Task.CompletedTask;

        // Image

        var imagePath = System.IO.Path.Combine(modRoot, "db", config.AvatarFile);
        if (!File.Exists(imagePath))
        {
            logger.LogError("[RZSM] CustomTrader: avatar file not found at '{Path}', aborting.", imagePath);
            return Task.CompletedTask;
        }
        imageRouter.AddRoute($"/files/trader/avatar/{CustomTraderConfig.TraderId}", imagePath);

        // TraderConfig (UpdateTime)

        var traderConfig = configServer.GetConfig<TraderConfig>();
        if (traderConfig.UpdateTime.All(x => x.TraderId != CustomTraderConfig.TraderId))
        {
            traderConfig.UpdateTime.Add(new UpdateTime
            {
                TraderId = CustomTraderConfig.TraderId,
                Seconds  = new MinMax<int>(3600, 3600)
            });
        }

        var ragfairConfig = configServer.GetConfig<RagfairConfig>();
        ragfairConfig.Traders.TryAdd(new MongoId(CustomTraderConfig.TraderId), true);

        // Build TraderBase from Prapor

        var traderBase = cloner.Clone(databaseService.GetTables().Traders[new MongoId("54cb50c76803fa8b248b4571")].Base);
        if (traderBase is null) {
            logger.LogError("[RZSM] Couldn't clone Prapor settings, aborting.");
            return Task.CompletedTask;
        }
        traderBase.Id = new MongoId(CustomTraderConfig.TraderId);
        traderBase.Avatar = $"/files/trader/avatar/{CustomTraderConfig.TraderId}.png";

        // Register in DB

        var traderEntry = new Trader
        {
            Base   = traderBase,
            Assort = new TraderAssort
            {
                Items           = [],
                BarterScheme    = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            },
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                { "Started", new Dictionary<MongoId, MongoId>() },
                { "Success", new Dictionary<MongoId, MongoId>() },
                { "Fail",    new Dictionary<MongoId, MongoId>() },
            },
            Dialogue = new Dictionary<string, List<string>?>
            {
                { "start",        [] },
                { "end",          [] },
                { "goodbuy",      [] },
                { "badass",       [] },
                { "cantafford",   [] },
                { "loyalty_gain", [] },
                { "barterReady",  [] },
            }
        };

        if (!databaseService.GetTables().Traders.TryAdd(traderBase.Id, traderEntry))
        {
            logger.LogWarning("[RZSM] CustomTrader: trader '{Id}' already exists in DB — skipping.", CustomTraderConfig.TraderId);
            return Task.CompletedTask;
        }

        // Seed locale keys

        foreach (var (_, locale) in databaseService.GetTables().Locales.Global)
        {
            locale.AddTransformer(data =>
            {
                data.TryAdd($"{CustomTraderConfig.TraderId} FullName", "CustomTraderName");
                data.TryAdd($"{CustomTraderConfig.TraderId} FirstName", "CustomTraderFirstName");
                data.TryAdd($"{CustomTraderConfig.TraderId} Nickname", "CustomTraderNickname");
                data.TryAdd($"{CustomTraderConfig.TraderId} Location", "CustomTraderLocation");
                data.TryAdd($"{CustomTraderConfig.TraderId} Description", "CustomTraderDescription");
                return data;
            });
        }

        var en = localeService.GetLocaleDb("en");
        en[$"{CustomTraderConfig.TraderId} FullName"] = "CustomTraderName";
        en[$"{CustomTraderConfig.TraderId} FirstName"] = "CustomTraderFirstName";
        en[$"{CustomTraderConfig.TraderId} Nickname"] = "CustomTraderNickname";
        en[$"{CustomTraderConfig.TraderId} Location"] = "CustomTraderLocation";
        en[$"{CustomTraderConfig.TraderId} Description"] = "CustomTraderDescription";

        logger.LogInformation("[RZSM] CustomTrader: Sable registered.");
        return Task.CompletedTask;
    }
}
