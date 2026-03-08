// RemzDNB - 2026

namespace RZCustomItemTiers;

// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// masterConfig.json
// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class MasterConfig
{
    public const string FileName = "masterConfig.json";

    public Dictionary<string, string> Tiers { get; set; } = new();
    public List<PriceThreshold> PriceThresholds { get; set; } = new();
}

public class PriceThreshold
{
    public int    MinPrice { get; set; }
    public string Tier     { get; set; } = "";
}

// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// categoryRules.json
// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class CategoryRulesConfig
{
    public const string FileName = "categoryRules.json";

    public Dictionary<string, string> Assignments { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// priceRules.json
// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class PriceRulesConfig
{
    public const string FileName = "priceRules.json";

    public bool Enabled { get; set; } = true;
    public List<string> Categories { get; set; } = new();
}

// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// Per-TPL override files
// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class OverrideFile
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, string> Overrides { get; set; } = new();
}
