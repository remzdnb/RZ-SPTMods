// RemzDNB - 2026

using System.Reflection;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace RZCustomTraders;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class CustomTraderPatcher(
    ILogger<CustomTraderPatcher> logger,
    ModHelper                    modHelper,
    ConfigLoader                 configLoader,
    TraderService                traderService
) : IOnLoad
{
    private const string SableId = "fa363fc015e07b5f7828c9f7";

    public Task OnLoad()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var modRoot = modHelper.GetAbsolutePathToModFolder(assembly);
        var masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName, assembly);

        if (!masterConfig.EnableCustomTrader) {
            return Task.CompletedTask;
        }

        if (!masterConfig.ForceApplyIds.Contains(SableId, StringComparer.OrdinalIgnoreCase))
        {
            logger.LogWarning("[RZCustomTraders] CustomTrader not in ForceApplyIds, skipping registration.");
            return Task.CompletedTask;
        }

        if (!masterConfig.AvatarOverrides.TryGetValue(SableId, out var avatarFile))
        {
            logger.LogError("[RZCustomTraders] CustomTrader : no AvatarOverrides entry found for '{Id}', aborting.", SableId);
            return Task.CompletedTask;
        }

        var imagePath = System.IO.Path.Combine(modRoot, "db", avatarFile);
        var traderBase = BuildTraderBase(imagePath);

        traderService.Register(traderBase, imagePath);

        return Task.CompletedTask;
    }

    private static TraderBase BuildTraderBase(string imagePath)
    {
        return new TraderBase
        {
            Id = new MongoId(SableId),
            Name = "X",
            Surname = "X",
            Nickname = "X",
            Location = "X",
            Avatar = $"/files/trader/avatar/{SableId}.png",

            // retarded variable names & uncommented code, that's all we like
            IsAvailableInPVE = true,
            AvailableInRaid = false,
            UnlockedByDefault = true,
            Currency = CurrencyType.RUB,
            BalanceRub = 100_000_000,
            GridHeight = 160,
            BuyerUp = false,
            Discount = 0,
            DiscountEnd = 0,
            NextResupply = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600,
            CustomizationSeller = false,
            Medic = false,
            SellCategory = [],
            // Placeholder, will be replaced by LoyaltyLevelsOverrides via ForceApplyIds.
            // At least one level is required or RagfairPriceService throws a null ref on startup.
            LoyaltyLevels =
            [
                new TraderLoyaltyLevel
                {
                    MinLevel = 1,
                    MinStanding = 0,
                    MinSalesSum = 0,
                    BuyPriceCoefficient = 0,
                    RepairPriceCoefficient = 0,
                    InsurancePriceCoefficient = 0
                }
            ],
            Insurance = new TraderInsurance
            {
                Availability = false,
                MinPayment = 0,
                MinReturnHour = 24,
                MaxReturnHour = 72,
                MaxStorageTime = 720
            },
            Repair = new TraderRepair
            {
                Availability = false,
                Quality = 0,
                Currency = "RUB",
                CurrencyCoefficient = 0,
                PriceRate = 0,
                ExcludedIdList = [],
                ExcludedCategory = []
            }
        };
    }
}

// nikoumouk le warning
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)]
public class TraderConfigPatcher(ConfigServer configServer) : IOnLoad
{
    private const string SableId = "fa363fc015e07b5f7828c9f7";

    public Task OnLoad()
    {
        var traderConfig = configServer.GetConfig<TraderConfig>();
        if (traderConfig.UpdateTime.All(x => x.TraderId != SableId))
        {
            traderConfig.UpdateTime.Add(new UpdateTime
            {
                TraderId = SableId,
                Seconds = new MinMax<int>(3600, 3600)
            });
        }

        return Task.CompletedTask;
    }
}
