namespace Evolver.Core.Entities;

public sealed class ProductionOrderMaterialLine : BaseEntity
{
    public long ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public long MaterialProductId { get; set; }
    public Product MaterialProduct { get; set; } = null!;

    public decimal PlannedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
}
