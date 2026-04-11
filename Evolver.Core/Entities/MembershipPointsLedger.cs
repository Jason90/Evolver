namespace Evolver.Core.Entities;

/// <summary>积分变动流水，可与订单关联。</summary>
public sealed class MembershipPointsLedger : BaseEntity
{
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public long? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public decimal PointDelta { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Reason { get; set; }
}
