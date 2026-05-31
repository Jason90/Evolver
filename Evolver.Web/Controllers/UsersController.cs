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
using Microsoft.Extensions.Logging;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    ITenantContext tenant,
    UserSpreadsheetService spreadsheet,
    ILogger<UsersController> logger
) : ControllerBase
{
    /// <param name="status">筛选：<c>all</c>（默认）、<c>active</c>、<c>inactive</c>。</param>
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.Query)]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List([FromQuery] string? status, [FromQuery] long? orgId, CancellationToken ct)
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

        HashSet<long>? orgScope = null;
        if (orgId is { } oid)
        {
            orgScope = await GetOrgSubtreeIdsAsync(oid, ct);
            if (orgScope.Count == 0)
                return Ok(Array.Empty<UserListItemDto>());
            var userIds = await db.UserOrganizations.AsNoTracking()
                .Where(x => orgScope.Contains(x.OrganizationId))
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(u => userIds.Contains(u.Id));
        }

        var users = await query.OrderBy(u => u.UserName).ToListAsync(ct);
        var userIdSet = users.Select(u => u.Id).ToHashSet();
        var userOrgMap = await db.UserOrganizations.AsNoTracking()
            .Where(x => userIdSet.Contains(x.UserId))
            .GroupBy(x => x.UserId)
            .Select(g => new { UserId = g.Key, OrgId = g.Select(z => z.OrganizationId).OrderBy(v => v).FirstOrDefault() })
            .ToDictionaryAsync(x => x.UserId, x => x.OrgId, ct);
        var orgIds = userOrgMap.Values.ToHashSet();
        var orgNameMap = await db.Organizations.AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        var list = new List<UserListItemDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            userOrgMap.TryGetValue(u.Id, out var did);
            orgNameMap.TryGetValue(did, out var dname);
            list.Add(new UserListItemDto(
                u.Id,
                u.UserName ?? "",
                u.Email,
                u.PhoneNumber,
                did == 0 ? null : did,
                dname,
                u.IsActive,
                roles.ToList(),
                u.UpdateTime,
                u.UpdateBy,
                null,
                u.Remark));
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
            orgIds.Count > 0 ? orgIds[0] : null,
            user.Remark,
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
            IsActive = dto.IsActive ?? true,
            Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim()
        };

        var res = await userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        if (dto.RoleNames is { Count: > 0 })
        {
            var roleNames = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
            await AssignTenantRolesAsync(user, roleNames, ct);
        }

        if (dto.DepartmentId is { } depId)
        {
            await db.UserOrganizations.AddAsync(new UserOrganization
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                UserId = user.Id,
                OrganizationId = depId
            }, ct);
            user.OrgId = depId <= int.MaxValue ? (int)depId : tenant.OrgId;
            await userManager.UpdateAsync(user);
            await db.SaveChangesAsync(ct);
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserListItemDto(user.Id, user.UserName ?? "", user.Email, user.PhoneNumber, dto.DepartmentId, null, user.IsActive, roles.ToList(), user.UpdateTime, user.UpdateBy, null, user.Remark));
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
        if (dto.Remark is not null)
            user.Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim();
        if (dto.IsActive is { } active)
            user.IsActive = active;
        if (dto.OrgId is { } orgId)
            user.OrgId = orgId;

        if (dto.DepartmentId is { } depId)
        {
            if (!await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == depId && o.TenantId == tenant.TenantId, ct))
                return BadRequest("目标组织不存在。");

            var existingOrgs = await db.UserOrganizations.Where(x => x.UserId == id).ToListAsync(ct);
            db.UserOrganizations.RemoveRange(existingOrgs);
            db.UserOrganizations.Add(new UserOrganization
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                UserId = id,
                OrganizationId = depId
            });

            if (depId <= int.MaxValue)
                user.OrgId = (int)depId;
        }

        var res = await userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        await db.SaveChangesAsync(ct);
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
                r => r.TenantId == tenant.TenantId && r.Name == rn && r.IsActive,
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

    [HttpPost("{id:long}/move-department")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.MoveDepartment)]
    public async Task<ActionResult> MoveDepartment(long id, [FromBody] MoveUserDepartmentDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        if (!await db.Organizations.AsNoTracking().AnyAsync(o => o.Id == dto.DepartmentId && o.TenantId == tenant.TenantId, ct))
            return BadRequest("目标组织不存在。");

        var existing = await db.UserOrganizations.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserOrganizations.RemoveRange(existing);
        db.UserOrganizations.Add(new UserOrganization
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            UserId = id,
            OrganizationId = dto.DepartmentId
        });

        if (dto.DepartmentId <= int.MaxValue)
            user.OrgId = (int)dto.DepartmentId;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("User {UserId} moved to department {DepartmentId} by {OperatorId}", id, dto.DepartmentId, tenant.UserId);
        return NoContent();
    }

    [HttpPost("{id:long}/password")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Users.ResetPassword)]
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

        logger.LogInformation("Password reset for user {UserId} by {OperatorId}", id, tenant.UserId);
        return NoContent();
    }

    private async Task<HashSet<long>> GetOrgSubtreeIdsAsync(long rootId, CancellationToken ct)
    {
        var all = await db.Organizations.AsNoTracking()
            .Where(o => o.TenantId == tenant.TenantId)
            .Select(o => new { o.Id, o.ParentId })
            .ToListAsync(ct);
        var childrenMap = all.GroupBy(x => x.ParentId).ToLookup(g => g.Key, g => g.Select(v => v.Id).ToList());
        var result = new HashSet<long>();
        var q = new Queue<long>();
        q.Enqueue(rootId);
        while (q.Count > 0)
        {
            var id = q.Dequeue();
            if (!result.Add(id))
                continue;
            foreach (var children in childrenMap[id])
            {
                foreach (var ch in children)
                    q.Enqueue(ch);
            }
        }
        return result;
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
