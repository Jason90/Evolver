namespace Evolver.Core.MultiTenancy;

public interface ITenantContext
{
    int TenantId { get; }
    int OrgId { get; }
    long? UserId { get; }
}
