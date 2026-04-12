// RemzDNB - 2026

namespace RZServerManager.Shared;

public enum TradeCurrency { Rub, Eur, Usd }

public class TplCount
{
    public string ItemTpl { get; set; } = "";
    public int Count { get; set; } = 1;
}

public class ChildItem
{
    public string ItemTpl { get; set; } = "";
    public string SlotId { get; set; } = "";
    public int Count { get; set; } = 1;
}
