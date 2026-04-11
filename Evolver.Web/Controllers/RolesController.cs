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
[Route("api/roles")]
[Authorize]
public sealed class RolesController(
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    ITenantContext tenant
) : ControllerBase
{
    [HttpGet]
    [RequirePermission("roles.read")]
    public ActionResult<IReadOnlyList<RoleDto>> Get()
    {
        var rows = roleManager.Roles
            .AsNoTracking()
            .Where(r => r.TenantId == tenant.TenantId && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name!))
            .ToList();

        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    [RequirePermission("roles.read")]
    public async Task<ActionResult<RoleDto>> GetById(long id, CancellationToken ct)
    {
        var role = await roleManager.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);
        if (role is null)
            return NotFound();
        return Ok(new RoleDto(role.Id, role.Name!));
    }

    [HttpGet("{roleId:long}/permissions")]
    [RequirePermission("roles.read")]
    public async Task<ActionResult<IReadOnlyList<long>>> GetPermissionIds(long roleId, CancellationToken ct)
    {
        var role = await roleManager.Roles.AsNoTracking()
            .AnyAsync(r => r.Id == roleId && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);
        if (!role)
            return NotFound();

        var ids = await db.RolePermissions.AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionId)
            .ToListAsync(ct);

        return Ok(ids);
    }

    [HttpPost]
    [RequirePermission("roles.write")]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        var appRole = new AppRole
        {
            Name = name,
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId
        };
        var res = await roleManager.CreateAsync(appRole);
        if (!res.Succeeded)
            return BadRequest(res.Errors.Select(e => e.Description));

        return Ok(new RoleDto(appRole.Id, appRole.Name!));
    }

    [HttpPut("{id:long}")]
    [RequirePermission("roles.write")]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateRoleDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(name))
            return BadRequest("Name is required.");

        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);
        if (role is null)
            return NotFound();

        role.Name = name;
        var res = await roleManager.UpdateAsync(role);
        if (!res.Succeeded)
            return BadRequest(res.Errors.Select(e => e.Description));

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission("roles.write")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);
        if (role is null)
            return NotFound();

        var links = await db.RolePermissions.Where(x => x.RoleId == id).ToListAsync(ct);
        db.RolePermissions.RemoveRange(links);

        role.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{roleId:long}/permissions")]
    [RequirePermission("roles.write")]
    public async Task<ActionResult> SetPermissions(long roleId, [FromBody] SetRolePermissionsDto dto, CancellationToken ct)
    {
        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);
        if (role is null)
            return NotFound();

        var existing = await db.RolePermissions.Where(x => x.RoleId == roleId).ToListAsync(ct);
        db.RolePermissions.RemoveRange(existing);

        var perms = await db.Permissions.Where(p => dto.PermissionIds.Contains(p.Id)).ToListAsync(ct);
        foreach (var p in perms)
        {
            db.RolePermissions.Add(new RolePermission
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                RoleId = roleId,
                PermissionId = p.Id
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
