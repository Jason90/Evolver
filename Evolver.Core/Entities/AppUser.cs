using Microsoft.AspNetCore.Identity;

namespace Evolver.Core.Entities;

public sealed class AppUser : IdentityUser<long>
{
    /// <summary>业务启用状态：false 表示停用（不可登录）。</summary>
    public bool IsActive { get; set; } = true;

    public int TenantId { get; set; } = 1;
    public int OrgId { get; set; } = 1;
    public string? Remark { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
