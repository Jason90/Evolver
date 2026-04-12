using System.Security.Claims;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Shared.Dtos;
using Evolver.Web.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Evolver.Web.Controllers;

/// <summary>当前用户账户（对标 Identity 管理页中的「我的帐户」能力，API + Blazor 前端）。</summary>
[ApiController]
[Route("api/account")]
[Authorize]
public sealed class AccountController(
    UserManager<AppUser> userManager,
    ITenantContext tenant,
    IOptions<PlatformOptions> platformOptions
) : ControllerBase
{
    private readonly PlatformOptions _platform = platformOptions.Value;

    [HttpGet("me")]
    public async Task<ActionResult<AccountProfileDto>> Me(CancellationToken ct)
    {
        var user = await FindCurrentUserAsync(ct);
        if (user is null)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var roleList = roles.ToList();
        var canProvision = _platform.PlatformTenantId == tenant.TenantId
            && roleList.Exists(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
        return Ok(new AccountProfileDto(
            user.Id,
            user.UserName ?? "",
            user.Email,
            user.PhoneNumber,
            user.TenantId,
            user.OrgId,
            user.IsActive,
            roleList,
            canProvision));
    }

    [HttpPost("change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangeOwnPasswordDto dto, CancellationToken ct)
    {
        var user = await FindCurrentUserAsync(ct);
        if (user is null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest("CurrentPassword and NewPassword are required.");

        var res = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    private async Task<AppUser?> FindCurrentUserAsync(CancellationToken ct)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(raw, out var id))
            return null;

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null || user.TenantId != tenant.TenantId)
            return null;

        return user;
    }
}
