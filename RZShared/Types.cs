// RemzDNB - 2026

namespace RZShared;

/// <summary>
/// Generic item entry used across all mods.
/// Not all fields are relevant in every context, unused fields are simply ignored.
/// </summary>
public class ItemEntry
{
    /// <summary>Required. Item template ID.</summary>
    public string Tpl { get; set; } = "";

    /// <summary>Stack count / quantity. Default: 1.</summary>
    public int Count { get; set; } = 1;

    /// <summary>Slot ID. Used for child items.</summary>
    public string? SlotId { get; set; }

    /// <summary>Price in roubles.</summary>
    public int Price { get; set; } = 0;

    /// <summary>Item durability percentage (0-100).</summary>
    public int Durability { get; set; } = 100;

    /// <summary>Weight for weighted loot pools. Default: 1.</summary>
    public int Weight { get; set; } = 1;

    /// <summary>Spawn chance percentage (0-100) for loot pools. Default: 100.</summary>
    public double Chance { get; set; } = 100;

    /// <summary>Minimum stack size. Used for loot pools. Default: 1.</summary>
    public int MinStack { get; set; } = 1;

    /// <summary>Maximum stack size. Used for loot pools. Default: 1.</summary>
    public int MaxStack { get; set; } = 1;

    /// <summary>Trader MongoDB ID. Used for trade overrides.</summary>
    public string? TraderId { get; set; }

    /// <summary>Trader loyalty level requirement. Default: 1.</summary>
    public int LoyaltyLevel { get; set; } = 1;
}
