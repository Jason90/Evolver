namespace Evolver.Core.Entities;

/// <summary>
/// Fast-query inventory snapshot per org/product (optional location). Authoritative movement is <see cref="InventoryTransaction"/>.
/// </summary>
public sealed class InventorySnapshot : InventoryConcurrencyEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Optional sub-location within org (stall code, bin). Empty string = default location (unique index).</summary>
    public string LocationCode { get; set; } = "";

    public decimal CurrentStock { get; set; }
    public decimal SafetyStock { get; set; }
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
}
