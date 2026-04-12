using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Infrastructure.Persistence.Identity;

/// <summary>
/// 按租户限定用户查找；并必须重写 <see cref="FindRoleAsync"/>。
/// 基类 <see cref="UserStore{TUser,TRole,TContext,TKey}.FindRoleAsync"/> 仅按 NormalizedName 查角色且使用 SingleOrDefault，
/// 多租户下多个 Admin 会在 AddToRoleAsync / IsInRoleAsync 等路径抛出「Sequence contains more than one element」。
/// </summary>
public sealed class TenantAwareUserStore : UserStore<AppUser, AppRole, AppDbContext, long>
{
    private readonly ITenantContext _tenant;

    public TenantAwareUserStore(
        AppDbContext context,
        IdentityErrorDescriber describer,
        ITenantContext tenant)
        : base(context, describer) =>
        _tenant = tenant;

    /// <summary>与基类不同：增加 <see cref="AppRole.TenantId"/> 条件，并避免 Single 对重复行的异常。</summary>
    protected override async Task<AppRole?> FindRoleAsync(
        string normalizedRoleName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(normalizedRoleName))
            return null;

        // 基类中 Roles 为 private，使用与之一致的 DbSet。
        return await Context.Set<AppRole>()
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(
                r => r.NormalizedName == normalizedRoleName && r.TenantId == _tenant.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task<AppUser?> FindByNameAsync(
        string normalizedUserName,
        CancellationToken cancellationToken = default)
    {
        return await Users
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(
                u => u.NormalizedUserName == normalizedUserName && u.TenantId == _tenant.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task<AppUser?> FindByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        return await Users
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(
                u => u.NormalizedEmail == normalizedEmail && u.TenantId == _tenant.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
