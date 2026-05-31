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

    /// <summary>是否激活；为 false 时列表不展示且名称可被新租户复用（见库索引）。</summary>
    public bool IsActive { get; set; } = true;

    public DateTime? ExpireAt { get; set; }
    public string? Remark { get; set; }

    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
