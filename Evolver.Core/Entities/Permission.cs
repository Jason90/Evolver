namespace Evolver.Core.Entities;

public enum PermissionType
{
    Api = 1,
    UiButton = 2
}

public sealed class Permission : BaseEntity
{
    public long? ParentId { get; set; }
    public Permission? Parent { get; set; }

    public PermissionType Type { get; set; }

    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Resource { get; set; }
}
