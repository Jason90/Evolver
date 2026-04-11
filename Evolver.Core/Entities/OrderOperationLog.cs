namespace Evolver.Core.Entities;

public sealed class OrderOperationLog : BaseEntity
{
    public long SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public string Action { get; set; } = "";
    public SalesOrderStatus? FromStatus { get; set; }
    public SalesOrderStatus? ToStatus { get; set; }
    public long? ActorUserId { get; set; }
    public AppUser? ActorUser { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? Detail { get; set; }
}
