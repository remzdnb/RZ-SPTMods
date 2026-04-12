// RemzDNB - 2026

namespace RZEssentials._Shared;

public class LogConfig : IConfig
{
    public static string FileName => "miscSettings/logConfig.json";

    public Dictionary<string, bool> Channels { get; set; } = new();
}

public enum TradeCurrency { Rub, Eur, Usd }

public class ItemEntry
{
    public string Tpl { get; set; } = "";
    public int Count { get; set; } = 1;
    public string? SlotId { get; set; } = null;
}

public class StatOperation
{
    public string Op { get; set; } = "set";
    public double Value { get; set; }
}
