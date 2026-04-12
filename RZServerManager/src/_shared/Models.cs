// RemzDNB - 2026

namespace RZServerManager._Shared;

public class MasterConfig : IConfig
{
    public static string FileName => "masterConfig.json";

    public bool EnableDevMode { get; set; } = false;
    public bool EnableDevLogs { get; set; } = false;
}

public enum TradeCurrency { Rub, Eur, Usd }

public class ItemEntry
{
    public string Tpl { get; set; } = "";
    public int Count { get; set; } = 1;
    public string? SlotId { get; set; } = null;
}
