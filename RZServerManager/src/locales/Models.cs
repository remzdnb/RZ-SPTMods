// RemzDNB - 2026

using RZServerManager._Shared;

namespace RZServerManager.Locales;

public record LocalesMainConfig : IConfig
{
    public static string FileName => "locales/localesMainConfig.json";

    public bool ForceEnglishItems { get; set; } = false;
    public bool ForceEnglishMaps { get; set; } = false;

    public bool EnableHideoutLocaleOverrides { get; set; } = false;
    public Dictionary<string, string> HideoutLocaleOverrides { get; set; } = new();

    public bool EnableTraderLocaleOverrides { get; set; } = false;
    public Dictionary<string, TraderLocaleEntry> TraderLocaleOverrides { get; set; } = new();
}

public record TraderLocaleEntry
{
    public string? FullName    { get; set; }
    public string? FirstName   { get; set; }
    public string? Nickname    { get; set; }
    public string? Location    { get; set; }
    public string? Description { get; set; }
}
