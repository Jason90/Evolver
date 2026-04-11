namespace Evolver.Core.Entities;

/// <summary>
/// Tenant (tenant master). <see cref="TenantId"/> mirrors <see cref="Id"/> for consistent multi-tenant columns; <see cref="OrgId"/> is 0 at tenant scope.
/// </summary>
public sealed class Tenant
{
    public int Id { get; set; }

    /// <summary>Same as <see cref="Id"/> for this row.</summary>
    public int TenantId { get; set; }

    /// <summary>Always 0 for tenant-level rows.</summary>
    public int OrgId { get; set; }

    public string Name { get; set; } = "";

    public bool IsDeleted { get; set; }

    public int? CreateBy { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
