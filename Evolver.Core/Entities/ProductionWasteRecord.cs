namespace Evolver.Core.Entities;

public sealed class ProductionWasteRecord : BaseEntity
{
    public long ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public string? Reason { get; set; }
}
