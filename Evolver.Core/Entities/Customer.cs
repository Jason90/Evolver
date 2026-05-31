namespace Evolver.Core.Entities;

public sealed class Customer : BaseEntity
{
    public long? CustomerCategoryRefId { get; set; }
    public CustomerCategory? CustomerCategory { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public CustomerType CustomerType { get; set; } = CustomerType.WalkIn;
    public string? Gender { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? MemberNo { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
