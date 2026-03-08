// RemzDNB - 2026

using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RZCustomProfiles;

public record MasterConfig
{
    public const string FileName = "masterConfig.json";
    public HashSet<int> EnabledBaseProfiles { get; set; } = new();
    public bool UnlockAllOutfits { get; set; } = false;
    public Dictionary<string, bool>? UnlockHideoutCustomizations { get; set; } = null;
    public bool UnlockJaeger { get; set; } = false;
    public bool UnlockRef { get; set; } = false;
    public List<CategoryEntry> ExaminedCategoryBlacklist { get; set; } = [];
    public List<string> ExaminedBlacklist { get; set; } = [];

    public static readonly Dictionary<int, string> BaseProfiles = new()
    {
        [0] = "Standard",
        [1] = "Left Behind",
        [2] = "Prepare To Escape",
        [3] = "Edge Of Darkness",
        [4] = "Unheard",
        [5] = "Tournament",
        [6] = "SPT Developer",
        [7] = "SPT Easy start",
        [8] = "SPT Zero to hero",
    };

    public static readonly Dictionary<int, string> SecureContainers = new()
    {
        [1] = ItemTpl.SECURE_WAIST_POUCH,
        [2] = ItemTpl.SECURE_CONTAINER_ALPHA,
        [3] = ItemTpl.SECURE_CONTAINER_BETA,
        [4] = ItemTpl.SECURE_CONTAINER_EPSILON,
        [5] = ItemTpl.SECURE_CONTAINER_GAMMA,
        [6] = ItemTpl.SECURE_CONTAINER_THETA,
        [7] = ItemTpl.SECURE_CONTAINER_KAPPA,
        [8] = ItemTpl.SECURE_CONTAINER_KAPPA_DESECRATED,
        [9] = ItemTpl.SECURE_CONTAINER_BOSS,
        [10] = ItemTpl.SECURE_CONTAINER_GAMMA_TUE,
        [11] = ItemTpl.SECURE_DEVELOPER_SECURE_CONTAINER,
        [12] = ItemTpl.SECURE_TOURNAMENT_SECURED_CONTAINER,
    };

    public static readonly Dictionary<string, (string CategoryId, string CustomisationType)> HideoutCategories = new()
    {
        ["Wall"]              = ("67373f1e5a5ee73f2a081baf", CustomisationType.WALL),
        ["Floor"]             = ("67373f170eca6e03ab0d5391", CustomisationType.FLOOR),
        ["Ceiling"]           = ("673b3f595bf6b605c90fcdc2", CustomisationType.CEILING),
        ["Light"]             = ("67373f286cadad262309e862", CustomisationType.LIGHT),
        ["MannequinPose"]     = ("675ff48ce8d2356707079617", CustomisationType.MANNEQUIN_POSE),
        ["ShootingRangeMark"] = ("67373f330eca6e03ab0d5394", CustomisationType.SHOOTING_RANGE_MARK),
        //["ItemSlot"]          = ("67373f520eca6e03ab0d5397", CustomisationType.CAT),
        //["PosterSlot"]        = ("67373f4b5a5ee73f2a081bb3", CustomisationType.CAT),
    };

    public static readonly HashSet<string> ProtectedSlots = new(StringComparer.OrdinalIgnoreCase)
    {
        "SecuredContainer", "Pockets", "Dogtag"
    };
}

public record ProfileConfig
{
    public bool Enabled { get; set; } = true;
    public int BaseProfile { get; set; } = 6;
    public string Name { get; set; } = "DefaultProfileName";
    public string? Description { get; set; }
    public bool AllItemsExamined { get; set; } = false;
    public bool MaxLevel { get; set; } = false;
    public int? StartingLevel { get; set; } = null;
    public int? StartingPrestigeLevel { get; set; } = null;
    public bool SkipPrestigeRewards { get; set; } = true;
    public bool UnlockAllAchievements { get; set; } = false;
    public bool MaxSkills { get; set; } = false;
    public Dictionary<string, float>? SkillOverrides { get; set; } = null;
    public Dictionary<string, TraderLoyaltyConfig>? TradersLoyalty { get; set; }
    public bool ClearStash { get; set; } = false;
    public bool ClearEquipment { get; set; } = false;
    public int SecureContainer { get; set; } = 0;
    public StartingItemsConfig? AdditionalStartingItems { get; set; }
    public Dictionary<string, int>? HideoutStartingLevels { get; set; }
}

public record TraderLoyaltyConfig
{
    public double Standing { get; set; } = 99.0;
    public double SalesSum { get; set; } = 1000000;
}

public record StartingItemsConfig
{
    public bool Enabled { get; set; } = true;
    public List<ItemEntry> Items { get; set; } = new();
}

public class ItemEntry
{
    public string Tpl { get; set; } = "";
    public int Count { get; set; } = 1;
}

public record CategoryEntry
{
    public string CategoryId { get; set; } = "";
    public bool Enabled { get; set; } = false;
}
