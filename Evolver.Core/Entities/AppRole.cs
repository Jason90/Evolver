using Microsoft.AspNetCore.Identity;

namespace Evolver.Core.Entities;

public sealed class AppRole : IdentityRole<long>
{
    public int TenantId { get; set; } = 1;
    public int OrgId { get; set; } = 1;

    /// <summary>是否激活：为 false 时不在列表与角色选择中出现，但保留行与审计字段。</summary>
    public bool IsActive { get; set; } = true;

    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
