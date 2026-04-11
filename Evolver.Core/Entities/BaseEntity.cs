namespace Evolver.Core.Entities;

public abstract class BaseEntity
{
    public long Id { get; set; }

    public int TenantId { get; set; }
    public int OrgId { get; set; }

    public bool IsDeleted { get; set; }

    public int? CreateBy { get; set; }
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public int? UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }
}
