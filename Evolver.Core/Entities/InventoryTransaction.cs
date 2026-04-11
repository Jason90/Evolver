namespace Evolver.Core.Entities;

/// <summary>Inventory ledger (single source of truth for stock movements).</summary>
public sealed class InventoryTransaction : BaseEntity
{
    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public InventoryTransactionType TransactionType { get; set; }
    public decimal Quantity { get; set; }
    public decimal BeforeQuantity { get; set; }
    public decimal AfterQuantity { get; set; }

    public InventorySourceType SourceType { get; set; }
    public long? SourceId { get; set; }

    public string? ReferenceNo { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
