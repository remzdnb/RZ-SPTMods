// RemzDNB - 2026

using System.Reflection;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using RZServerManager._Shared;

namespace RZServerManager.Traders;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class FencePatcher(
    ILogger<FencePatcher> logger,
    DatabaseService databaseService,
    FenceService fenceService,
    ConfigServer configServer,
    ConfigLoader configLoader,
    AssortUtilities assortUtilities,
    RandomUtil randomUtil
) : IOnLoad
{
    private static MasterConfig? _masterConfig;
    private static SupplyConfig? _supplyConfig;

    private static FencePatcher? _fencePatcher;
    private static FenceService? _fenceService;
    private static FenceConfig?  _fenceConfig;
    private static int?          _restockSeconds;

    public Task OnLoad()
    {
        _fencePatcher = this;
        _fenceService = fenceService;
        _fenceConfig = configLoader.Load<FenceConfig>(FenceConfig.FileName);
        _masterConfig = configLoader.Load<MasterConfig>(MasterConfig.FileName);
        _supplyConfig = configLoader.Load<SupplyConfig>(SupplyConfig.FileName);

        var harmony = new Harmony("com.rz.customeconomy.fence");
        harmony.Patch(
            AccessTools.Method(typeof(FenceService), nameof(FenceService.GenerateFenceAssorts)),
            postfix: new HarmonyMethod(typeof(GenerateFenceAssortsPostFix), nameof(GenerateFenceAssortsPostFix.Postfix))
        );

        // Supply

        if (_supplyConfig.EnableSupplyConfig &&
            _supplyConfig.EnableRestockTimes &&
            _supplyConfig.RestockTimes.TryGetValue(SPTarkov.Server.Core.Models.Enums.Traders.FENCE, out var fenceSeconds))
        {
            configServer.GetConfig<TraderConfig>().Fence.PartialRefreshTimeSeconds = fenceSeconds;
            _restockSeconds = fenceSeconds;
        }

        // Item pools.

        if (!_fenceConfig.EnableFenceConfig) {
            return Task.CompletedTask;
        }

        var fence = configServer.GetConfig<TraderConfig>().Fence;

        if (!_fenceConfig.EnableDefaultItemPool)
        {
            fence.AssortSize                            = 0;
            fence.WeaponPresetMinMax                    = new MinMax<int> { Min = 0, Max = 0 };
            fence.EquipmentPresetMinMax                 = new MinMax<int> { Min = 0, Max = 0 };
            fence.DiscountOptions.AssortSize            = 0;
            fence.DiscountOptions.WeaponPresetMinMax    = new MinMax<int> { Min = 0, Max = 0 };
            fence.DiscountOptions.EquipmentPresetMinMax = new MinMax<int> { Min = 0, Max = 0 };

            return Task.CompletedTask;
        }

        fence.AssortSize
            = _fenceConfig.AssortSize ?? fence.AssortSize;
        fence.WeaponPresetMinMax
            = _fenceConfig.WeaponPresetMinMax ?? fence.WeaponPresetMinMax;
        fence.EquipmentPresetMinMax
            = _fenceConfig.EquipmentPresetMinMax ?? fence.EquipmentPresetMinMax;
        fence.ItemPriceMult
            = _fenceConfig.ItemPriceMultiplier ?? fence.ItemPriceMult;
        fence.PresetPriceMult
            = _fenceConfig.PresetPriceMultiplier ?? fence.PresetPriceMult;
        fence.DiscountOptions.AssortSize
            = _fenceConfig.DiscountAssortSize ?? fence.DiscountOptions.AssortSize;
        fence.DiscountOptions.WeaponPresetMinMax
            = _fenceConfig.DiscountWeaponPresetMinMax ?? fence.DiscountOptions.WeaponPresetMinMax;
        fence.DiscountOptions.EquipmentPresetMinMax
            = _fenceConfig.DiscountEquipmentPresetMinMax ?? fence.DiscountOptions.EquipmentPresetMinMax;
        fence.DiscountOptions.ItemPriceMult
            = _fenceConfig.DiscountItemPriceMultiplier ?? fence.DiscountOptions.ItemPriceMult;
        fence.DiscountOptions.PresetPriceMult
            = _fenceConfig.DiscountPresetPriceMultiplier ?? fence.DiscountOptions.PresetPriceMult;

        if (_fenceConfig.EnableDefaultItemPoolAdditions)
        {
            var fenceDbAssort = databaseService.GetTrader(SPTarkov.Server.Core.Models.Enums.Traders.FENCE)?.Assort;
            if (fenceDbAssort is not null)
            {
                foreach (var item in _fenceConfig.DefaultItemPoolAdditions)
                {
                    var rootItemAndChildren = assortUtilities.CreateRootItem(item.Tpl);
                    var rootItem = rootItemAndChildren.First();

                    fenceDbAssort.Items.AddRange(rootItemAndChildren);
                    fenceDbAssort.BarterScheme[rootItem.Id] = [
                        [
                            new BarterScheme {
                                Template = ItemTpl.MONEY_ROUBLES,
                                Count = item.RoublePrice
                            }
                        ]
                    ];
                    fenceDbAssort.LoyalLevelItems[rootItem.Id] = item.LoyaltyLevel;
                }
            }
        }

        return Task.CompletedTask;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // InjectOffers
    // ─────────────────────────────────────────────────────────────────────────

    private void InjectCustomOffers()
    {
        if (_fenceConfig == null || _fenceConfig.CustomItemPool.Count == 0) {
            return;
        }

        var fenceAssort = fenceService.GetMainFenceAssort();
        if (fenceAssort is null) {
            return;
        }

        var pool = _fenceConfig.CustomItemPool
            .Where(o => o.Weight > 0 && !string.IsNullOrWhiteSpace(o.Tpl))
            .SelectMany(o => Enumerable.Repeat(o, o.Weight))
            .ToList();

        if (pool.Count == 0) {
            return;
        }

        var injected = 0;
        var templates = databaseService.GetTables().Templates?.Items;
        for (var i = 0; i < _fenceConfig.CustomItemCount; i++)
        {
            var offer = pool[randomUtil.GetInt(0, pool.Count - 1)];

            if (templates is null || !templates.ContainsKey(offer.Tpl))
            {
                logger.LogWarning("[RZCustomEconomy] Fence offer TPL '{Tpl}' not found, skipping.", offer.Tpl);
                continue;
            }

            var items = assortUtilities.CreateRootItem(offer.Tpl, offer.StackSize, resolveChildren: true);
            var rootItem = items.First();

            fenceAssort.Items.AddRange(items);
            var price = (int)Math.Max(1, Math.Round(offer.Price * _fenceConfig.CustomItemPriceMultiplier));
            fenceAssort.BarterScheme[rootItem.Id] =
                [[new BarterScheme { Template = offer.CurrencyTpl, Count = price }]];
            fenceAssort.LoyalLevelItems[rootItem.Id] = offer.LoyaltyLevel;
            injected++;
        }

        if (_masterConfig is not null && _masterConfig.EnableDevLogs) {
            logger.LogInformation("\e[1;32m[RZCustomEconomy] {Count} custom fence offer(s) injected.\e[0m", injected);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Harmony postfix — tourne APRES GenerateFenceAssorts
    // ─────────────────────────────────────────────────────────────────────────

    private static class GenerateFenceAssortsPostFix
    {
        public static void Postfix()
        {
            if (_masterConfig != null && _fenceConfig.EnableFenceConfig && _fenceConfig != null && _fenceConfig.EnableCustomItemPool)
            {
                _fencePatcher?.InjectCustomOffers();
            }

            // Corrige NextResupply sur l'assort — c'est ce que le client affiche comme timer.
            // SPT le calcule dans CreateFenceAssortSkeleton() avec l'ancien PartialRefreshTimeSeconds,
            // donc on l'écrase ici après coup avec la bonne valeur.
            if (_masterConfig != null && _supplyConfig.EnableSupplyConfig && _restockSeconds.HasValue && _fenceService is not null)
            {
                var main = _fenceService.GetMainFenceAssort();
                if (main is not null)
                    main.NextResupply = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + _restockSeconds.Value;
            }
        }
    }
}
