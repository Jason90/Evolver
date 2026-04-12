using System.ComponentModel.DataAnnotations;
using Evolver.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Security;

/// <summary>
/// 默认 <see cref="UserValidator{T}"/> 按全局用户名判重；多租户下应在同一 <see cref="AppUser.TenantId"/> 内判重。
/// </summary>
public sealed class TenantScopedUserValidator : IUserValidator<AppUser>
{
    public async Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
    {
        var errors = new List<IdentityError>();

        await ValidateUserNameAsync(manager, user, errors).ConfigureAwait(false);
        await ValidateEmailAsync(manager, user, errors).ConfigureAwait(false);

        return errors.Count > 0 ? IdentityResult.Failed(errors.ToArray()) : IdentityResult.Success;
    }

    private static async Task ValidateUserNameAsync(
        UserManager<AppUser> manager,
        AppUser user,
        List<IdentityError> errors)
    {
        var name = user.UserName;
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new IdentityError
            {
                Code = "UserNameId",
                Description = "User name cannot be empty."
            });
            return;
        }

        var normalized = manager.NormalizeName(name);
        if (string.IsNullOrEmpty(normalized))
        {
            errors.Add(new IdentityError
            {
                Code = "InvalidUserName",
                Description = $"User name '{name}' is invalid."
            });
            return;
        }

        var owner = await manager.Users.FirstOrDefaultAsync(u =>
                u.TenantId == user.TenantId &&
                u.NormalizedUserName == normalized)
            .ConfigureAwait(false);

        if (owner is not null && owner.Id != user.Id)
        {
            errors.Add(new IdentityError
            {
                Code = "DuplicateUserName",
                Description = $"Username '{name}' is already taken."
            });
        }
    }

    private async Task ValidateEmailAsync(
        UserManager<AppUser> manager,
        AppUser user,
        List<IdentityError> errors)
    {
        var email = user.Email;
        if (string.IsNullOrWhiteSpace(email))
            return;

        if (!new EmailAddressAttribute().IsValid(email))
        {
            errors.Add(new IdentityError
            {
                Code = "InvalidEmail",
                Description = $"Email '{email}' is invalid."
            });
            return;
        }

        var normalized = manager.NormalizeEmail(email);
        if (string.IsNullOrEmpty(normalized))
            return;

        // 与 RequireUniqueEmail 解耦：同一租户内邮箱不重复（跨租户可相同）。
        var owner = await manager.Users.FirstOrDefaultAsync(u =>
                u.TenantId == user.TenantId &&
                u.NormalizedEmail == normalized)
            .ConfigureAwait(false);

        if (owner is not null && owner.Id != user.Id)
        {
            errors.Add(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = $"Email '{email}' is already taken."
            });
        }
    }
}
