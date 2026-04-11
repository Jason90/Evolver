using System.Security.Claims;
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
    AppDbContext db,
    ITenantContext tenant
) : ControllerBase
{
    [HttpGet]
    [RequirePermission("users.read")]
    public async Task<ActionResult<IReadOnlyList<UserListItemDto>>> List(CancellationToken ct)
    {
        var users = await userManager.Users
            .AsNoTracking()
            .Where(u => u.TenantId == tenant.TenantId && !u.IsDeleted)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);

        var list = new List<UserListItemDto>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            list.Add(new UserListItemDto(u.Id, u.UserName ?? "", u.Email, u.PhoneNumber, roles.ToList()));
        }

        return Ok(list);
    }

    [HttpGet("{id:long}")]
    [RequirePermission("users.read")]
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
            user.OrgId,
            roles.ToList(),
            orgIds));
    }

    [HttpPost]
    [RequirePermission("users.write")]
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
            OrgId = tenant.OrgId
        };

        var res = await userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        if (dto.RoleNames is { Count: > 0 })
        {
            var roleNames = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
            var addRoles = await userManager.AddToRolesAsync(user, roleNames);
            if (!addRoles.Succeeded)
                return BadRequest(string.Join("; ", addRoles.Errors.Select(e => e.Description)));
        }

        var roles = await userManager.GetRolesAsync(user);
        return Ok(new UserListItemDto(user.Id, user.UserName ?? "", user.Email, user.PhoneNumber, roles.ToList()));
    }

    [HttpPut("{id:long}")]
    [RequirePermission("users.write")]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email.Trim();
        if (dto.PhoneNumber is not null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        if (dto.OrgId is { } orgId)
            user.OrgId = orgId;

        var res = await userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("users.write")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        if (GetCurrentUserId() == id)
            return BadRequest("Cannot delete the current user.");

        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        user.IsDeleted = true;
        var res = await userManager.UpdateAsync(user);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    [HttpPost("{id:long}/roles")]
    [RequirePermission("users.write")]
    public async Task<ActionResult> SetRoles(long id, [FromBody] SetUserRolesDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var current = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, current);
        if (!remove.Succeeded)
            return BadRequest(string.Join("; ", remove.Errors.Select(e => e.Description)));

        var next = dto.RoleNames.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).Distinct().ToList();
        if (next.Count > 0)
        {
            var add = await userManager.AddToRolesAsync(user, next);
            if (!add.Succeeded)
                return BadRequest(string.Join("; ", add.Errors.Select(e => e.Description)));
        }

        return NoContent();
    }

    [HttpPost("{id:long}/organizations")]
    [RequirePermission("users.write")]
    public async Task<ActionResult> SetOrganizations(long id, [FromBody] SetUserOrganizationsDto dto, CancellationToken ct)
    {
        var user = await FindUserInTenantAsync(id, ct);
        if (user is null)
            return NotFound();

        var existing = await db.UserOrganizations.Where(x => x.UserId == id).ToListAsync(ct);
        db.UserOrganizations.RemoveRange(existing);

        foreach (var oid in dto.OrganizationIds.Distinct())
        {
            if (!await db.Organizations.AnyAsync(o => o.Id == oid && !o.IsDeleted, ct))
                continue;

            db.UserOrganizations.Add(new UserOrganization
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                UserId = id,
                OrganizationId = oid,
                IsDeleted = false
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:long}/password")]
    [RequirePermission("users.write")]
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
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == id && u.TenantId == tenant.TenantId && !u.IsDeleted, ct);
        return user;
    }

    private long? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(raw, out var id) ? id : null;
    }
}
