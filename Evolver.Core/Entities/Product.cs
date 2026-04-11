namespace Evolver.Core.Entities;

public sealed class Product : BaseEntity
{
    public long? ProductCategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";

    public decimal UnitPrice { get; set; }
    public decimal? UnitCost { get; set; }

    /// <summary>Suggested list price (may mirror UnitPrice).</summary>
    public decimal? SuggestedPrice { get; set; }

    /// <summary>Denormalized / cached; authoritative stock is <see cref="InventorySnapshot"/>.</summary>
    public decimal TheoreticalStock { get; set; }

    public decimal ActualStock { get; set; }

    /// <summary>Typically AvgDailySales × 1.2 per requirements.</summary>
    public decimal AlertStock { get; set; }

    public decimal? CostAmount { get; set; }
}
