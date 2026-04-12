using Evolver.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Security;

/// <summary>
/// 默认 <see cref="RoleValidator{T}"/> 按全局 NormalizedName 判重，多租户下每个租户都需要名为 Admin 的角色。
/// 本验证器仅在同一 <see cref="AppRole.TenantId"/> 内保证角色名唯一。
/// </summary>
public sealed class TenantScopedRoleValidator : IRoleValidator<AppRole>
{
    public async Task<IdentityResult> ValidateAsync(RoleManager<AppRole> manager, AppRole role)
    {
        if (string.IsNullOrWhiteSpace(role.Name))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "RoleNameEmpty",
                Description = "Role name cannot be empty."
            });
        }

        var normalized = manager.NormalizeKey(role.Name);
        if (string.IsNullOrEmpty(normalized))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidRoleName",
                Description = "Role name is invalid."
            });
        }

        var duplicate = await manager.Roles.AnyAsync(r =>
            r.TenantId == role.TenantId &&
            r.NormalizedName == normalized &&
            r.Id != role.Id);

        if (duplicate)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateRoleName",
                Description = $"Role name '{role.Name}' is already taken."
            });
        }

        return IdentityResult.Success;
    }
}
