// RemzDNB - 2026

namespace RZCustomProfiles;

public record MasterConfig
{
    public const string FileName = "masterConfig.json";
    public HashSet<int> EnabledBaseProfiles { get; set; } = new();
    public bool UnlockAllOutfits { get; set; } = false;

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

    public static readonly Dictionary<int, string> Pockets = new()
    {
        [1] = ItemTpl.POCKETS_1X3,
        [2] = ItemTpl.POCKETS_1X4_TUE,
        [3] = ItemTpl.POCKETS_2X3,
        [4] = ItemTpl.POCKETS_LARGE,
    };

    public static readonly HashSet<string> ProtectedSlots = new(StringComparer.OrdinalIgnoreCase)
    {
        "SecuredContainer", "Pockets", "Dogtag"
    };
}

public record ProfileConfig
{
    public string Name { get; set; } = "DefaultProfileName";
    public string? Description { get; set; }
    public int BaseProfile { get; set; } = 6;
    public bool MaxLevel { get; set; } = false;
    public bool MaxSkills { get; set; } = false;
    public Dictionary<string, TraderLoyaltyConfig>? TradersLoyalty { get; set; }
    public bool ClearStash { get; set; } = true;
    public bool ClearEquipment { get; set; } = false;
    public int SecureContainer { get; set; } = 0;
    public int Pockets { get; set; } = 0;
    public StartingItemsConfig? StartingItems { get; set; }
    public Dictionary<string, int>? HideoutStartingLevels { get; set; }
}

public record TraderLoyaltyConfig
{
    public double Standing { get; set; } = 99.0;
    public double SalesSum { get; set; } = 1000000;
    public bool Unlocked { get; set; } = true;
}

public record StartingItemsConfig
{
    public bool Enabled { get; set; } = true;
    public List<BaseItem> Items { get; set; } = new();
}

public record BaseItem
{
    public string Tpl { get; set; } = "";
    public int Count { get; set; } = 1;
}
