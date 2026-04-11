namespace Evolver.Core.Entities;

public sealed class Organization : BaseEntity
{
    public long? ParentId { get; set; }
    public Organization? Parent { get; set; }

    public string Name { get; set; } = "";
    public string? OrgType { get; set; }
}
