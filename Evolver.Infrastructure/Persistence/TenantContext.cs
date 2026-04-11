using Evolver.Core.MultiTenancy;

namespace Evolver.Infrastructure.Persistence;

public sealed class TenantContext : ITenantContext
{
    public int TenantId { get; set; } = 1;
    public int OrgId { get; set; } = 1;
    public long? UserId { get; set; }
}
