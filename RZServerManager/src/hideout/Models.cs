// RemzDNB - 2026

using SPTarkov.Server.Core.Models.Enums.Hideout;
using RZServerManager._Shared;

namespace RZServerManager.Hideout;

// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// hideoutConfig.json
// ─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class HideoutConfig : IConfig
{
    public static string FileName => "hideout/hideoutConfig.json";
    public bool EnableHideoutConfig { get; set; } = false;

    public Dictionary<string, HideoutAreaConfig> Areas { get; init; } = new();
    public BitcoinFarmConfig? BitcoinFarm { get; set; }
    public bool? RequireFoundInRaid { get; set; } = null;
}

public class HideoutAreaConfig
{
    public bool? RemoveFromDb { get; set; }
    public bool? Enabled { get; set; }
    public bool? DisplayLevel { get; set; }

    public bool UseCustomConstructionTime { get; set; } = false;
    public Dictionary<string, double> ConstructionTime { get; set; } = new();
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

public class CraftingConfig : IConfig
{
    public static string FileName => "hideout/craftingConfig.json";
    public bool EnableCraftingConfig { get; set; } = false;

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
