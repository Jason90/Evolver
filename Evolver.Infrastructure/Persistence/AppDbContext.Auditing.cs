using Evolver.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence;

public sealed partial class AppDbContext
{
    public override int SaveChanges()
    {
        ApplyAuditInfo();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditInfo()
    {
        var utc = DateTime.UtcNow;
        var uid = _tenant.UserId;
        int? createBy = uid is null ? null : (uid.Value <= int.MaxValue ? (int)uid.Value : null);
        int? updateBy = createBy;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateTime = utc;
                    entry.Entity.CreateBy = createBy;
                    entry.Entity.UpdateTime = null;
                    entry.Entity.UpdateBy = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateTime = utc;
                    entry.Entity.UpdateBy = updateBy;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Tenant>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateTime = utc;
                    entry.Entity.CreateBy = createBy;
                    entry.Entity.UpdateTime = null;
                    entry.Entity.UpdateBy = null;
                    if (entry.Entity.TenantId == 0)
                        entry.Entity.TenantId = entry.Entity.Id;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateTime = utc;
                    entry.Entity.UpdateBy = updateBy;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateTime = utc;
                    entry.Entity.CreateBy = createBy;
                    entry.Entity.UpdateTime = null;
                    entry.Entity.UpdateBy = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateTime = utc;
                    entry.Entity.UpdateBy = updateBy;
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AppRole>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreateTime = utc;
                    entry.Entity.CreateBy = createBy;
                    entry.Entity.UpdateTime = null;
                    entry.Entity.UpdateBy = null;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdateTime = utc;
                    entry.Entity.UpdateBy = updateBy;
                    break;
            }
        }
    }
}
