// RemzDNB - 2026

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Enums.Hideout;

namespace RZCustomEconomy;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Shared primitives
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

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

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Trader IDs
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public static class TraderIds
{
    public const string Prapor = "54cb50c76803fa8b248b4571";
    public const string Therapist = "54cb57776803fa99248b456e";
    public const string Fence = "579dc571d53a0658a154fbec";
    public const string Skier = "58330581ace78e27b8b10cee";
    public const string Peacekeeper = "5935c25fb3acc3127c3d8cd9";
    public const string Mechanic = "5a7c2eca46aef81a7ca2145d";
    public const string Ragman = "5ac3b934156ae10c4430e83c";
    public const string Jaeger = "5c0647fdd443bc2504c2d371";
    public const string Caretaker = "638f541a29ffd1183d187f57";
    public const string Btr = "656f0f98d80a697f855d34b1";
    public const string Arena = "6617beeaa9cfa777ca915b7c";
    public const string Storyteller = "6864e812f9fe664cb8b8e152";

    public static string? FromName(string name)
    {
        return typeof(TraderIds).GetField(name)?.GetValue(null) as string;
    }
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Trader sell config
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public enum ItemSellMode
{
    /// <summary>Leave the trader's vanilla sell config untouched.</summary>
    Default,

    /// <summary>Trader does not buy anything from the player.</summary>
    Disabled,

    /// <summary>Trader buys items belonging to the specified handbook categories.</summary>
    Categories,

    /// <summary>Trader buys all handbook items except those in the blacklist.</summary>
    AllWithBlacklist,
}

public class TraderSellConfig
{
    public ItemSellMode Mode { get; set; } = ItemSellMode.Default;

    /// <summary>Handbook category IDs. Used when Mode is Categories.</summary>
    public List<string> Categories { get; set; } = new();

    /// <summary>Item TPLs the trader will not buy. Used when Mode is AllWithBlacklist.</summary>
    public List<string> Blacklist { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// masterConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class MasterConfig
{
    public const string FileName = "masterConfig.json";

    public bool ClearDefaultAssorts { get; set; } = true;
    public bool ClearFenceAssorts { get; set; } = true;
    public bool EnableAutoRouting { get; set; } = true;
    public bool EnableManualOffers { get; set; } = true;
    public bool EnableBuybackConfig { get; set; } = true;
    public bool EnableHideoutConfig { get; set; } = true;
    public bool EnableCraftingConfig { get; set; } = true;
    public bool DisableFleaMarket { get; set; } = true;
    public bool AllItemsExamined { get; set; } = false;
    public bool UnlockAllTraders { get; set; } = false;
    public bool EnableDevMode { get; set; } = false;
    public bool EnableDevLogs { get; set; } = false;

    public Dictionary<string, int> HandbookPrices { get; set; } = new();
    public Dictionary<string, TraderSellConfig> TraderSellConfigs { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// autoRoutingConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class CategoryRoute
{
    public bool Enabled { get; set; } = true;
    public string CategoryId { get; set; } = "";
    public string TraderName { get; set; } = "";
    public double PriceMultiplier { get; set; } = 1.0;
    public int LoyaltyLevel { get; set; } = 1;
}

public class AutoTradeOverride
{
    public string ItemTpl { get; set; } = "";
    public string TraderName { get; set; } = "";
    public int PriceRoubles { get; set; } = 0;
    public double PriceMultiplier { get; set; } = 1.0;
    public int LoyaltyLevel { get; set; } = 1;
    public int StackCount { get; set; } = -1;
    public List<BarterItem> BarterItems { get; set; } = new();
}

public class AutoRoutingConfig
{
    public const string FileName = "autoRoutingConfig.json";

    public bool ForceRouteAll { get; set; } = false;
    public bool RouteModdedItemsOnly { get; set; } = false;
    public bool RouteVanillaItemsOnly { get; set; } = false;
    public bool EnableOverrides { get; set; } = true;
    public bool UseStaticBlacklist { get; set; } = true;
    public bool UseUserBlacklist { get; set; } = true;
    public string? FallbackTrader { get; set; } = null;

    // Items that are broken, invisible, or non-functional in-game.
    // Never sold at any trader AND never added to the encyclopedia.
    public List<string> StaticBlacklist { get; set; } = new();

    // Items you want to hide from traders.
    // Never sold at any trader, but still added to the encyclopedia.
    public List<string> UserBlacklist { get; set; } = new();

    public List<CategoryRoute> CategoryRoutes { get; set; } = new();
    public List<AutoTradeOverride> Overrides { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// manualOffersConfig.json
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

public class ManualOffersConfig
{
    public const string FileName = "manualOffersConfig.json";

    public List<ManualTraderOffers> ManualOffers { get; set; } = new();
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
// hideoutConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class HideoutConfig
{
    public const string FileName = "hideoutConfig.json";

    public Dictionary<string, HideoutAreaConfig> Areas { get; init; } = new();
    public BitcoinFarmConfig? BitcoinFarm { get; set; }
}

public class StageRequirement
{
    public string Type { get; set; } = "";

    // Pour Type = "Item"
    public string? ItemTpl { get; set; }
    public int ItemCount { get; set; } = 1;
    public bool ItemFunctional { get; set; } = false;

    // Pour Type = "Area"
    public string? AreaName { get; set; }
    public int AreaLevel { get; set; } = 1;

    // Pour Type = "Skill"
    public string? SkillName { get; set; }
    public int SkillLevel { get; set; } = 1;

    // Pour Type = "TraderLoyalty"
    public string? TraderName { get; set; }
    public int TraderLoyalty { get; set; } = 1;

    // Pour Type = "QuestComplete"
    public string? QuestId { get; set; }
}

public class HideoutAreaConfig
{
    public bool? RemoveFromDb { get; set; }
    public bool? Enabled { get; set; }
    public bool? DisplayLevel { get; set; }
    public int? StartingLevel { get; set; } = null;
    public Dictionary<string, List<StageRequirement>>? LevelRequirements { get; set; }
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
    public HideoutAreas AreaType { get; set; }
    public int AreaLevel { get; set; } = 1;
    public string EndProduct { get; set; } = "";
    public int Count { get; set; } = 1;
    public int ProductionTime { get; set; } = 0;
    public bool NeedFuelForAllProductionTime { get; set; } = false;
    public bool Locked { get; set; } = false;
    public bool Continuous { get; set; } = false;
    public List<CraftRequirement> Requirements { get; set; } = new();
}

public class CraftsConfig
{
    public const string FileName = "craftingConfig.json";
    public List<HideoutAreas> ClearAreas { get; set; } = new();
    public Dictionary<HideoutAreas, List<CraftRecipe>> Recipes { get; set; } = new();
}

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Config Loader
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

[Injectable]
public class ConfigLoader(ILogger<ConfigLoader> logger)
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private static readonly string _configDir = Path.Combine(AppContext.BaseDirectory, "user", "mods", "RZCustomEconomy", "config");

    private readonly Dictionary<Type, object> _cachedConfigs = new();

    public T Load<T>(string filename)
        where T : new()
    {
        if (_cachedConfigs.TryGetValue(typeof(T), out var cached))
            return (T)cached;

        var path = Path.Combine(_configDir, filename);
        if (!File.Exists(path))
        {
            logger.LogWarning("[RZCustomEconomy] {File} not found — using default config.", filename);
            var def = new T();
            _cachedConfigs[typeof(T)] = def;
            return def;
        }

        var result = JsonSerializer.Deserialize<T>(File.ReadAllText(path), _options) ?? new T();
        _cachedConfigs[typeof(T)] = result;
        return result;
    }
}
