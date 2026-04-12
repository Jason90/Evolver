using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence.Identity;

/// <summary>
/// 默认 <see cref="RoleStore{TRole,TContext,TKey}"/> 仅按 NormalizedName 查询；多租户下多个 Admin 会命中多行，
/// 若同一租户内因历史原因存在重复行，应使用 First 语义，避免 Single 抛错。
/// </summary>
public sealed class TenantAwareRoleStore : RoleStore<AppRole, AppDbContext, long>
{
    private readonly ITenantContext _tenant;

    public TenantAwareRoleStore(
        AppDbContext context,
        IdentityErrorDescriber describer,
        ITenantContext tenant)
        : base(context, describer) =>
        _tenant = tenant;

    public override async Task<AppRole?> FindByNameAsync(
        string normalizedRoleName,
        CancellationToken cancellationToken = default)
    {
        // 使用 First：历史数据若在同一租户出现重复行，仍应返回一条而不是抛错。
        return await Roles
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(
                r => r.NormalizedName == normalizedRoleName && r.TenantId == _tenant.TenantId && !r.IsDeleted,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
