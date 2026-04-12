namespace Evolver.Core.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }

    public int TenantId { get; set; }
    public int OrgId { get; set; }

    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
