using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Evolver.Infrastructure.Persistence;

public sealed partial class AppDbContext
{
    /// <summary>
    /// 所有继承 <see cref="BaseEntity"/> 的实体：当前 <see cref="ITenantContext.TenantId"/> + <see cref="ITenantContext.OrgId"/>。
    /// 跨租户/全组织查询请使用 <c>IgnoreQueryFilters()</c>。
    /// </summary>
    private void ApplyTenantOrgFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType is null)
                continue;

            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            var tenantProp = Expression.Property(parameter, nameof(BaseEntity.TenantId));
            var orgProp = Expression.Property(parameter, nameof(BaseEntity.OrgId));

            var tenantValue = Expression.Field(Expression.Constant(this), "_tenant");
            var tenantIdValue = Expression.Property(tenantValue, nameof(ITenantContext.TenantId));
            var orgIdValue = Expression.Property(tenantValue, nameof(ITenantContext.OrgId));

            var tenantEq = Expression.Equal(tenantProp, tenantIdValue);
            var orgEq = Expression.Equal(orgProp, orgIdValue);

            Expression body = Expression.AndAlso(tenantEq, orgEq);
            if (entityType.ClrType == typeof(Unit))
            {
                var activeProp = Expression.Property(parameter, nameof(Unit.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }
            else if (entityType.ClrType == typeof(ProductCategory))
            {
                var activeProp = Expression.Property(parameter, nameof(ProductCategory.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }
            else if (entityType.ClrType == typeof(Product))
            {
                var activeProp = Expression.Property(parameter, nameof(Product.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }
            else if (entityType.ClrType == typeof(CustomerCategory))
            {
                var activeProp = Expression.Property(parameter, nameof(CustomerCategory.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }
            else if (entityType.ClrType == typeof(Customer))
            {
                var activeProp = Expression.Property(parameter, nameof(Customer.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }
            else if (entityType.ClrType == typeof(SystemParameter))
            {
                var activeProp = Expression.Property(parameter, nameof(SystemParameter.IsActive));
                body = Expression.AndAlso(body, Expression.Equal(activeProp, Expression.Constant(true)));
            }

            var lambda = Expression.Lambda(body, parameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// <see cref="Tenant"/> 不按 <see cref="BaseEntity"/> 建模，单独过滤：当前租户主键。
    /// </summary>
    private void ApplyTenantRowQueryFilter(ModelBuilder modelBuilder)
    {
        var parameter = Expression.Parameter(typeof(Tenant), "t");
        var idProp = Expression.Property(parameter, nameof(Tenant.Id));
        var activeProp = Expression.Property(parameter, nameof(Tenant.IsActive));
        var tenantValue = Expression.Field(Expression.Constant(this), "_tenant");
        var tenantIdValue = Expression.Property(tenantValue, nameof(ITenantContext.TenantId));
        var idEq = Expression.Equal(idProp, tenantIdValue);
        var isActive = Expression.Equal(activeProp, Expression.Constant(true));
        var body = Expression.AndAlso(idEq, isActive);
        modelBuilder.Entity<Tenant>().HasQueryFilter(Expression.Lambda(body, parameter));
    }
}
