namespace Evolver.Core.Entities;

public sealed class Customer : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public CustomerType CustomerType { get; set; } = CustomerType.WalkIn;
    public string? Phone { get; set; }
    public string? MemberNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
