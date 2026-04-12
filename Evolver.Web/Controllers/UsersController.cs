using System.Security.Claims;
using Evolver.Web.Services;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    ITenantContext tenant,
    UserSpreadsheetService spreadsheet
) : ControllerBase
{
    /// <param name="status">筛选：<c>all</c>（默认）、<c>active</c>、<c>inactive</c>。</param>
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Query)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List([FromQuery] string? status, CancellationToken ct)
    {
        var query = userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenant.TenantId);

        query = status?.ToLowerInvariant() switch
        {
            "active" => query.Where(u => u.IsActive),
            "inactive" => query.Where(u => !u.IsActive),
            _ => query
        };

        var users = await query.OrderBy(u => u.UserName).ToListAsync(ct);

        var list = new List<UserListItemDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            list.Add(new UserListItemDto(u.Id, u.UserName ?? "", u.Email, u.PhoneNumber, u.IsActive, roles.ToList()));
        }

        return Ok(list);
    }

    [HttpGet("export")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Export)]
    public async Task<IActionResult> Export([FromQuery] string? status, CancellationToken ct)
    {
        await using var stream = await spreadsheet.ExportAsync(status, ct);
        var fileName = $"users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<UserImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Query)]
    public async Task<ActionResult<UserDetailDto>> GetById(long id, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var orgIds = await db.UserOrganizations.AsNoTracking()
            .Where(x => x.UserId == id)
            .Select(x => x.OrganizationId)
            .ToListAsync(ct);

        return Ok(new UserDetailDto(
            user.Id,
            user.UserName ?? "",
            user.Email,
            user.PhoneNumber,
            user.IsActive,
            user.OrgId,
            roles.ToList(),
            orgIds));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Create)]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var userName = dto.UserName.Trim();
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(dto.Password))
            return BadRequest("UserName and Password are required.");

        var user = new AppUser
        {
            UserName = userName,
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            EmailConfirmed = true,
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            IsActive = dto.IsActive ?? true
        };

        var res = await userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        if (dto.RoleNames is { Count: > 0 })
        {
            var roleNames = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
            await AssignTenantRolesAsync(user, roleNames, ct);
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserListItemDto(user.Id, user.UserName ?? "", user.Email, user.PhoneNumber, user.IsActive, roles.ToList()));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email.Trim();
        if (dto.PhoneNumber is not null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        if (dto.IsActive is { } active)
            user.IsActive = active;
        if (dto.OrgId is { } orgId)
            user.OrgId = orgId;

        var res = await userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        if (GetCurrentUserId() == id)
            return BadRequest("Cannot delete the current user.");

        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var userOrgs = await db.UserOrganizations.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserOrganizations.RemoveRange(userOrgs);
        await db.SaveChangesAsync(ct);

        var res = await userManager.DeleteAsync(user);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    [HttpPost("{id:long}/roles")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Update)]
    public async Task<ActionResult> SetRoles(long id, [FromBody] SetUserRolesDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        await ClearUserRolesAsync(user, ct);

        var next = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
        if (next.Count > 0)
            await AssignTenantRolesAsync(user, next, ct);

        return NoContent();
    }

    private async Task ClearUserRolesAsync(AppUser user, CancellationToken ct)
    {
        var links = await db.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync(ct);
        db.UserRoles.RemoveRange(links);
        await db.SaveChangesAsync(ct);
    }

    private async Task AssignTenantRolesAsync(AppUser user, IReadOnlyList<string> roleNames, CancellationToken ct)
    {
        foreach (var rn in roleNames)
        {
            var role = await roleManager.Roles.FirstOrDefaultAsync(
                r => r.TenantId == tenant.TenantId && r.Name == rn && !r.IsDeleted,
                ct);
            if (role is null)
                continue;
            db.UserRoles.Add(new IdentityUserRole<long> { UserId = user.Id, RoleId = role.Id });
        }

        await db.SaveChangesAsync(ct);
    }

    [HttpPost("{id:long}/organizations")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Update)]
    public async Task<ActionResult> SetOrganizations(long id, [FromBody] SetUserOrganizationsDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var existing = await db.UserOrganizations.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserOrganizations.RemoveRange(existing);

        foreach (var oid in dto.OrganizationIds.Distinct())
        {
            if (!await db.Organizations.AnyAsync(o => o.Id == oid, ct))
                continue;

            db.UserOrganizations.Add(new UserOrganization
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                UserId = id,
                OrganizationId = oid
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:long}/password")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Update)]
    public async Task<ActionResult> AdminChangePassword(long id, [FromBody] AdminChangePasswordDto dto, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest("NewPassword must be at least 6 characters.");

        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var res = await userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    private async Task<AppUser?> FindUserInTenantAsync(long id, CancellationToken ct)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenant.TenantId, ct);
        return user;
    }

    private long? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var id) ? id : null;
    }
}
