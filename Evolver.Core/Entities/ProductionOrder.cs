namespace Evolver.Core.Entities;

public sealed class ProductionOrder : BaseEntity
{
    public string OrderNo { get; set; } = "";
    public long OutputProductId { get; set; }
    public Product OutputProduct { get; set; } = null!;

    public ProductionOrderStatus Status { get; set; } = ProductionOrderStatus.Draft;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public long? SourceSalesOrderId { get; set; }
    public SalesOrder? SourceSalesOrder { get; set; }

    public string? Notes { get; set; }

    public ICollection<ProductionOrderMaterialLine> MaterialLines { get; set; } = new List<ProductionOrderMaterialLine>();
    public ICollection<ProductionWasteRecord> WasteRecords { get; set; } = new List<ProductionWasteRecord>();
}
