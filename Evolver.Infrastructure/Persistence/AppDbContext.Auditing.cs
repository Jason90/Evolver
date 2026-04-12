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
        int? auditUserId = uid is null ? null : (uid.Value <= int.MaxValue ? (int)uid.Value : null);

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateTime = utc;
                entry.Entity.UpdateBy = auditUserId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Tenant>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateTime = utc;
                entry.Entity.UpdateBy = auditUserId;
            }

            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
                entry.Entity.TenantId = entry.Entity.Id;
        }

        foreach (var entry in ChangeTracker.Entries<AppUser>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateTime = utc;
                entry.Entity.UpdateBy = auditUserId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AppRole>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdateTime = utc;
                entry.Entity.UpdateBy = auditUserId;
            }
        }
    }
}
