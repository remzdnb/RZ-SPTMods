// RemzDNB - 2026

using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace RZCustomTraders;

// ─────────────────────────────────────────────────────────────────────────────
// masterConfig.json
// ─────────────────────────────────────────────────────────────────────────────

public record MasterConfig
{
    public const string FileName = "masterConfig.json";

    public bool EnableCustomTrader { get; set; } = false;
    public bool EnableAvatarOverrides { get; set; } = false;
    public bool EnableLocaleOverrides { get; set; } = false;
    public bool EnableLoyaltyLevels { get; set; } = false;
    public bool EnableRepairOverrides { get; set; } = false;

    public Dictionary<string, string> AvatarOverrides { get; set; } = new();
    public Dictionary<string, TraderLocaleEntry> LocaleOverrides { get; set; } = new();
    public Dictionary<string, List<TraderLoyaltyLevel>> LoyaltyLevelsOverrides { get; set; } = new();
    public Dictionary<string, TraderRepair> RepairOverrides { get; set; } = new();
    public List<string> ForceApplyIds { get; set; } = new();
}

public record TraderLocaleEntry
{
    public string? FullName    { get; set; }
    public string? FirstName   { get; set; }
    public string? Nickname    { get; set; }
    public string? Location    { get; set; }
    public string? Description { get; set; }
}
