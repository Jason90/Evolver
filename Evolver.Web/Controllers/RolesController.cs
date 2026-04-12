using Evolver.Core.Entities;

using Evolver.Core.MultiTenancy;

using Evolver.Infrastructure.Persistence;

using Evolver.Shared.Dtos;

using Evolver.Web.Security;

using Evolver.Web.Services;

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

    ITenantContext tenant,

    RoleSpreadsheetService spreadsheet

) : ControllerBase

{

    [HttpGet]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Query)]

    public async Task<ActionResult<IReadOnlyList<RoleDto>>> Get(CancellationToken ct)

    {

        var roles = await roleManager.Roles

            .AsNoTracking()

            .Where(r => r.TenantId == tenant.TenantId && !r.IsDeleted)

            .OrderBy(r => r.Name)

            .ToListAsync(ct);



        var orgIds = roles.Select(r => (long)r.OrgId).Distinct().ToList();

        var orgRows = await db.Organizations.AsNoTracking()

            .Where(o => orgIds.Contains(o.Id))

            .Select(o => new { o.Id, o.Name })

            .ToListAsync(ct);

        var orgMap = orgRows.ToDictionary(x => x.Id, x => x.Name);



        var list = roles.Select(r =>

        {

            var oid = (long)r.OrgId;

            orgMap.TryGetValue(oid, out var orgName);

            return new RoleDto(r.Id, r.Name!, r.TenantId, r.OrgId, orgName, r.NormalizedName, r.UpdateBy, r.UpdateTime);

        }).ToList();



        return Ok(list);

    }



    [HttpGet("export")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Export)]

    public async Task<IActionResult> Export(CancellationToken ct)

    {

        await using var stream = await spreadsheet.ExportAsync(ct);

        var fileName = $"roles_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

        return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);

    }



    [HttpPost("import")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Import)]

    [RequestSizeLimit(20 * 1024 * 1024)]

    public async Task<ActionResult<RoleImportResultDto>> Import(IFormFile? file, CancellationToken ct)

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

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Query)]

    public async Task<ActionResult<RoleDetailDto>> GetById(long id, CancellationToken ct)

    {

        var role = await roleManager.Roles

            .AsNoTracking()

            .FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);

        if (role is null)

            return NotFound();

        return Ok(new RoleDetailDto(role.Id, role.Name!, role.TenantId, role.OrgId));

    }



    [HttpGet("{roleId:long}/permissions")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Query)]

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

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Create)]

    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto dto, CancellationToken ct)

    {

        var name = dto.Name.Trim();

        if (string.IsNullOrEmpty(name))

            return BadRequest("Name is required.");



        var orgId = dto.OrgId ?? tenant.OrgId;

        if (!await IsOrgInTenantAsync(orgId, ct))

            return BadRequest("组织不存在或不属于当前租户。");



        var appRole = new AppRole

        {

            Name = name,

            TenantId = tenant.TenantId,

            OrgId = orgId

        };

        var res = await roleManager.CreateAsync(appRole);

        if (!res.Succeeded)

            return BadRequest(res.Errors.Select(e => e.Description));



        var orgName = await OrgNameAsync(orgId, ct);

        return Ok(new RoleDto(appRole.Id, appRole.Name!, appRole.TenantId, appRole.OrgId, orgName, appRole.NormalizedName, appRole.UpdateBy, appRole.UpdateTime));

    }



    [HttpPut("{id:long}")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Update)]

    public async Task<ActionResult> Update(long id, [FromBody] UpdateRoleDto dto, CancellationToken ct)

    {

        var name = dto.Name.Trim();

        if (string.IsNullOrEmpty(name))

            return BadRequest("Name is required.");



        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);

        if (role is null)

            return NotFound();



        if (dto.OrgId is { } oid)

        {

            if (!await IsOrgInTenantAsync(oid, ct))

                return BadRequest("组织不存在或不属于当前租户。");

            role.OrgId = oid;

        }



        role.Name = name;

        var res = await roleManager.UpdateAsync(role);

        if (!res.Succeeded)

            return BadRequest(res.Errors.Select(e => e.Description));



        return NoContent();

    }



    [HttpDelete("{id:long}")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Delete)]

    public async Task<ActionResult> Delete(long id, CancellationToken ct)

    {

        var role = await roleManager.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenant.TenantId && !r.IsDeleted, ct);

        if (role is null)

            return NotFound();



        var userRoles = await db.UserRoles.Where(ur => ur.RoleId == id).ToListAsync(ct);

        db.UserRoles.RemoveRange(userRoles);



        var links = await db.RolePermissions.Where(x => x.RoleId == id).ToListAsync(ct);

        db.RolePermissions.RemoveRange(links);

        await db.SaveChangesAsync(ct);



        role.IsDeleted = true;

        var upd = await roleManager.UpdateAsync(role);

        if (!upd.Succeeded)

            return BadRequest(string.Join("; ", upd.Errors.Select(e => e.Description)));



        return NoContent();

    }



    [HttpPost("{roleId:long}/permissions")]

    [RequirePermission(NavSystemSettingsPermissionCodes.Roles.Update)]

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



    private async Task<bool> IsOrgInTenantAsync(int orgId, CancellationToken ct) =>

        await db.Organizations.AsNoTracking()

            .AnyAsync(o => o.Id == orgId && o.TenantId == tenant.TenantId, ct);



    private async Task<string?> OrgNameAsync(int orgId, CancellationToken ct)

    {

        var oid = (long)orgId;

        return await db.Organizations.AsNoTracking()

            .Where(o => o.Id == oid)

            .Select(o => o.Name)

            .FirstOrDefaultAsync(ct);

    }

}


