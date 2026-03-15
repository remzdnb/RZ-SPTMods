// RemzDNB - 2026

namespace RZCustomItemTiers;

// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
// masterConfig.json
// ──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────

public class MasterConfig
{
    public const string FileName = "masterConfig.json";

    public string Theme { get; set; } = "dark";
    public Dictionary<string, Dictionary<string, string>> ThemePresets { get; set; } = new();
    public List<PriceThreshold> PriceThresholds { get; set; } = new();
    public bool EnableVerboseLogging { get; set; } = false;

    public Dictionary<string, string> GetTiers()
    {
        if (ThemePresets.TryGetValue(Theme, out var preset))
            return preset;

        return ThemePresets.Values.FirstOrDefault() ?? new Dictionary<string, string>();
    }
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
