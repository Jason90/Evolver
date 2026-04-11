namespace Evolver.Core.Entities;

public sealed class SalesOrder : BaseEntity
{
    public string OrderNo { get; set; } = "";

    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>Salesperson (user).</summary>
    public long SalesUserId { get; set; }
    public AppUser SalesUser { get; set; } = null!;

    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.PendingProduction;
    public DateTime OrderTime { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }

    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
    public ICollection<OrderOperationLog> OperationLogs { get; set; } = new List<OrderOperationLog>();
}
