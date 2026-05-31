namespace Evolver.Core.Entities;

public sealed class CustomerCategory : BaseEntity
{
    public string CategoryCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Remark { get; set; }
    public bool IsActive { get; set; } = true;
}
