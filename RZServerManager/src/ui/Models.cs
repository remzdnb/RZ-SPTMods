// RemzDNB - 2026

using RZServerManager._Shared;

namespace RZServerManager.UI;

public record UIConfig : IConfig
{
    public static string FileName => "ui/uiConfig.json";

    public bool ForceEnglishItems { get; set; } = false;
    public bool ForceEnglishMaps { get; set; } = false;

    public bool EnableAvatarOverrides { get; set; } = false;
    public Dictionary<string, string> AvatarOverrides { get; set; } = new();

    public bool EnableTraderLocaleOverrides { get; set; } = false;
    public Dictionary<string, TraderLocaleEntry> TraderLocaleOverrides { get; set; } = new();

    public bool EnableHideoutLocaleOverrides { get; set; } = false;
    public Dictionary<string, string> HideoutLocaleOverrides { get; set; } = new();
}

public record TraderLocaleEntry
{
    public string? FullName    { get; set; }
    public string? FirstName   { get; set; }
    public string? Nickname    { get; set; }
    public string? Location    { get; set; }
    public string? Description { get; set; }
}
