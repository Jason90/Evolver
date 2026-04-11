namespace Evolver.Core.Entities;

/// <summary>Per-market inventory snapshot (legacy / analytics). Uses row-version concurrency.</summary>
public sealed class MarketInventorySnapshot : InventoryConcurrencyEntity
{
    public DateOnly AsOfDate { get; set; }

    public long MarketId { get; set; }
    public Market Market { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal CurrentStock { get; set; }
    public decimal ParLevel { get; set; }
    public decimal ReorderPoint { get; set; }
    public string ReorderNeeded { get; set; } = "";
    public decimal GapToPar { get; set; }
}
