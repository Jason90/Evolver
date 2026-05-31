namespace Evolver.Core.Entities;

public sealed class Market : BaseEntity
{
    public string Name { get; set; } = "";
    public decimal RentAmount { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
