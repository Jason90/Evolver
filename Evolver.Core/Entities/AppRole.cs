using Microsoft.AspNetCore.Identity;

namespace Evolver.Core.Entities;

public sealed class AppRole : IdentityRole<long>
{
    public int TenantId { get; set; } = 1;
    public int OrgId { get; set; } = 1;
    public bool IsDeleted { get; set; }

    public int? CreateBy { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
