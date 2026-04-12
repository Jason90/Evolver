using Microsoft.AspNetCore.Identity;

namespace Evolver.Core.Entities;

public sealed class AppRole : IdentityRole<long>
{
    public int TenantId { get; set; } = 1;
    public int OrgId { get; set; } = 1;

    /// <summary>逻辑删除：为 true 时不在列表与角色选择中出现，但保留行与审计字段。</summary>
    public bool IsDeleted { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
