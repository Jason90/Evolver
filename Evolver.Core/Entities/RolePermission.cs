namespace Evolver.Core.Entities;

public sealed class RolePermission : BaseEntity
{
    public long RoleId { get; set; }
    public AppRole Role { get; set; } = null!;

    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
