// RemzDNB - 2026

using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums.Hideout;

namespace RZCustomEconomy;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// masterConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class MasterConfig
{
    public const string FileName = "masterConfig.json";

    public bool EnableDefaultTrades { get; set; } = true;
    public bool EnableRoutedTrades { get; set; } = false;
    public bool EnableManualTrades { get; set; } = false;
    public bool EnableFenceConfig { get; set; } = false;
    public bool EnableBuybackConfig { get; set; } = false;
    public bool EnableSupplyConfig { get; set; } = false;
    public bool EnableHideoutConfig { get; set; } = false;
    public bool EnableCraftingConfig { get; set; } = false;
    public bool EnableInsuranceConfig { get; set; } = false;
    public bool EnableHandbookPricesConfig { get; set; } = false;
    public bool EnableFleaMarketConfig { get; set; } = false;

    public bool EnableDevMode { get; set; } = false;
    public bool EnableDevLogs { get; set; } = false;
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// defaultTradesConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public record NoBarterTraderEntry
{
    public bool Enabled { get; set; } = false;
    public TradeCurrency Currency { get; set; } = TradeCurrency.Rub;
    public List<string> ExcludedBarterTpls { get; set; } = new();
}

public record DefaultTradesConfig
{
    public const string FileName = "defaultTradesConfig.json";

    public Dictionary<string, double> PriceMultipliers { get; set; } = new();
    public Dictionary<string, NoBarterTraderEntry> NoBarterTraders { get; set; } = new();
    public List<string> Blacklist { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// autoRoutingConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class RoutedTradesConfig
{
    public const string FileName = "routedTradesConfig.json";

    public bool ForceRouteAll { get; set; } = false;
    public bool RouteModdedItemsOnly { get; set; } = false;
    public bool RouteVanillaItemsOnly { get; set; } = false;
    public Dictionary<string, TradeCurrency> TraderCurrencies { get; set; } = new();
    public string? FallbackTrader { get; set; } = null;
    public List<string> Blacklist { get; set; } = [];

    public Dictionary<string, List<CategoryRoute>> CategoryRoutes { get; set; } = new();
}

public class CategoryRoute
{
    public bool Enabled { get; set; } = true;
    public string CategoryId { get; set; } = "";
    public double PriceMultiplier { get; set; } = 1.0;
    public int LoyaltyLevel { get; set; } = 1;
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// manualTradesConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class TradeOffer
{
    public string ItemTpl { get; set; } = "";
    public int StackCount { get; set; } = 1;
    public int LoyaltyLevel { get; set; } = 1;
    public int Durability { get; set; } = 100;
    public int PriceRoubles { get; set; } = 0;
    public List<ChildItem> Children { get; set; } = new();
    public List<BarterItem> BarterItems { get; set; } = new();
}

public class ManualTraderOffers
{
    public string Id { get; set; } = ""; // trader id
    public List<TradeOffer> Offers { get; set; } = new();
}

public class ManualTradesConfig
{
    public const string FileName = "manualTradesConfig.json";
    public List<ManualTraderOffers> ManualOffers { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────
// fenceConfig.json
// ─────────────────────────────────────────────────────────────────────────────

public class FenceConfig
{
    public const string FileName = "fenceConfig.json";

    // Default item pool settings.
    public bool EnableDefaultItemPool { get; set; } = false;

    public int? AssortSize { get; set; } = null;
    public MinMax<int>? WeaponPresetMinMax { get; set; } = null;
    public MinMax<int>? EquipmentPresetMinMax { get; set; } = null;
    public double? ItemPriceMultiplier { get; set; } = 1.0;
    public double? PresetPriceMultiplier { get; set; } = 1.0;

    public int? DiscountAssortSize { get; set; } = null;
    public MinMax<int>? DiscountWeaponPresetMinMax { get; set; } = null;
    public MinMax<int>? DiscountEquipmentPresetMinMax { get; set; } = null;
    public double? DiscountItemPriceMultiplier { get; set; } = 1.0;
    public double? DiscountPresetPriceMultiplier { get; set; } = 1.0;

    // Default item pool additions settings.
    public bool EnableDefaultItemPoolAdditions { get; set; } = false;
    public List<FencePoolItem> DefaultItemPoolAdditions { get; set; } = new();

    // Custom item pool.
    public bool EnableCustomItemPool { get; set; } = false;
    public int CustomItemCount { get; set; } = 10;
    public double CustomItemPriceMultiplier { get; set; } = 1.0;
    public List<FenceOffer> CustomItemPool { get; set; } = new();
}

public class FencePoolItem
{
    public string Tpl { get; set; } = "";
    public int RoublePrice { get; set; } = 0;
    public int LoyaltyLevel { get; set; } = 1;
}

public class FenceOffer
{
    public string Tpl { get; set; } = "";
    public int Weight { get; set; } = 1;
    public int StackSize { get; set; } = 1;
    public string CurrencyTpl { get; set; } = ItemTpl.MONEY_ROUBLES;
    public int Price { get; set; } = 1;
    public int LoyaltyLevel { get; set; } = 1;
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// buybackConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public enum BuybackMode
{
    Default,            // Leave the trader's vanilla buyback config untouched.
    Disabled,           // Trader does not buy anything from the player.
    Categories,         // Trader buys items belonging to the specified handbook category IDs.
    AllWithBlacklist,   // Trader buys all handbook items except those in the blacklist.
}

public class BuybackRule
{
    public BuybackMode Mode { get; set; } = BuybackMode.Default;

    // Handbook category IDs. Used when Mode is Categories.
    public List<string> Categories { get; set; } = new();

    // Item TPLs the trader will not buy. Used when Mode is AllWithBlacklist.
    public List<string> Blacklist { get; set; } = new();
}

public class BuybackConfig
{
    public const string FileName = "buybackConfig.json";

    public Dictionary<string, BuybackRule> Rules { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// supplyConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class SupplyConfig
{
    public const string FileName = "supplyConfig.json";

    public bool EnableRestockTimes { get; set; } = true;
    public Dictionary<string, int> RestockTimes { get; set; } = new();
    public StockMultipliersConfig StockMultipliers { get; set; } = new();
}

public class StockMultipliersConfig
{
    public bool EnableByTrader { get; set; } = false;
    public Dictionary<string, double> ByTrader { get; set; } = new();
    public bool EnableByCategory { get; set; } = false;
    public Dictionary<string, double> ByCategory { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// fleaMarketConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public record FleaMarketConfig
{
    public const string FileName = "fleaMarketConfig.json";

    public bool Disable { get; set; } = false;
    public List<string> DynamicForceDisable { get; set; } = [];
    public List<string> DynamicForceEnable { get; set; } = [];
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// hideoutConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class HideoutConfig
{
    public const string FileName = "hideoutConfig.json";

    public Dictionary<string, HideoutAreaConfig> Areas { get; init; } = new();
    public BitcoinFarmConfig? BitcoinFarm { get; set; }
    public bool? RequireFoundInRaid { get; set; } = null;
}

public class HideoutAreaConfig
{
    // Removes the area from the database entirely.
    public bool? RemoveFromDb { get; set; }

    // Whether the area is available to the player.
    public bool? Enabled { get; set; }

    // Whether the current level is displayed in the UI.
    public bool? DisplayLevel { get; set; }

    // ── Construction time ─────────────────────────────────────────────────────────────────────────────────────────────────────────────
    // If true, applies ConstructionTime to every stage. Vanilla values are untouched if false.

    public bool UseCustomConstructionTime { get; set; } = false;
    public Dictionary<string, double> ConstructionTime { get; set; } = new();

    // ── Custom requirements ───────────────────────────────────────────────────────────────────────────────────────────────────────────
    // If Use* is true: vanilla requirements of that type are cleared on every stage, then the matching dict is injected per stage.
    // If Use* is false: vanilla requirements of that type are left completely untouched.
    // Keys are stage level strings ("1", "2", "3"...). Missing stages are silently skipped.

    public bool UseCustomItemRequirements { get; set; } = false;
    public Dictionary<string, List<ItemRequirement>> ItemRequirements { get; set; } = new();

    public bool UseCustomAreaRequirements { get; set; } = false;
    public Dictionary<string, List<AreaRequirement>> AreaRequirements { get; set; } = new();

    public bool UseCustomSkillRequirements { get; set; } = false;
    public Dictionary<string, List<SkillRequirement>> SkillRequirements { get; set; } = new();

    public bool UseCustomTraderRequirements { get; set; } = false;
    public Dictionary<string, List<TraderRequirement>> TraderRequirements { get; set; } = new();
}

public class ItemRequirement
{
    public string ItemTpl { get; set; } = "";
    public int ItemCount { get; set; } = 1;
    public bool ItemFunctional { get; set; } = false;
}

public class AreaRequirement
{
    public string AreaName { get; set; } = "";
    public int AreaLevel { get; set; } = 1;
}

public class SkillRequirement
{
    public string SkillName { get; set; } = "";
    public int SkillLevel { get; set; } = 1;
}

public class TraderRequirement
{
    public string TraderName { get; set; } = "";
    public int TraderLoyalty { get; set; } = 1;
}

public class BitcoinFarmConfig
{
    public double? ProductionSpeedMultiplier { get; set; }
    public double? GpuBoostRate { get; set; }
    public int? MaxCapacity { get; set; }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// craftingConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class CraftingConfig
{
    public const string FileName = "craftingConfig.json";
    public List<HideoutAreas> ClearAreas { get; set; } = new();
    public Dictionary<HideoutAreas, List<CraftRecipe>> Recipes { get; set; } = new();
}

public class CraftRequirement
{
    public string Type { get; set; } = "Item"; // "Item" or "Area"
    public string? TemplateId { get; set; } // if Type == Item
    public HideoutAreas? AreaType { get; set; } // if Type == Area
    public int? RequiredLevel { get; set; } // if Type == Area
    public int Count { get; set; } = 1;
    public bool IsFunctional { get; set; } = false;
    public bool IsEncoded { get; set; } = false;
}

public class CraftRecipe
{
    public string EndProduct { get; set; } = "";
    public int Count { get; set; } = 1;
    public int ProductionTime { get; set; } = 0;
    public bool NeedFuelForAllProductionTime { get; set; } = false;
    public bool Locked { get; set; } = false;
    public bool Continuous { get; set; } = false;
    public List<CraftRequirement> Requirements { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// insuranceConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public record InsuranceConfig
{
    public const string FileName = "insuranceConfig.json";

    public bool DisableAll { get; set; } = false;
    public List<CategoryBlacklistEntry> CategoryBlacklist { get; set; } = [];
    public List<string> TplBlacklist { get; set; } = [];
}

public record CategoryBlacklistEntry
{
    public bool Enabled { get; set; } = false;
    public string CategoryId { get; set; } = "";
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// handbookPricesConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class HandbookPricesConfig
{
    public const string FileName = "handbookPricesConfig.json";

    public Dictionary<string, int> Prices { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Shared primitives
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public enum TradeCurrency { Rub, Eur, Usd }

public class BarterItem
{
    public string ItemTpl { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class ChildItem
{
    public string ItemTpl { get; set; } = "";
    public string SlotId { get; set; } = "";
    public int Count { get; set; } = 1;
}
