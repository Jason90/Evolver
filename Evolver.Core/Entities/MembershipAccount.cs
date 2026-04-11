namespace Evolver.Core.Entities;

/// <summary>Membership: points, stored value, discounts — linked to customer.</summary>
public sealed class MembershipAccount : BaseEntity
{
    public long CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal PointsBalance { get; set; }
    public decimal StoredValueBalance { get; set; }
    public decimal DiscountRate { get; set; }
    public bool IsActive { get; set; } = true;
}
