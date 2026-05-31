using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Evolver.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/units")]
[Authorize]
public sealed class UnitsController(AppDbContext db, ITenantContext tenant, UnitSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Query)]
    public async Task<ActionResult<IReadOnlyList<UnitListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.Units
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .ToListAsync(ct);

        var userIds = rows
            .Select(x => x.UpdateBy)
            .Where(x => x is not null)
            .Select(x => (long)x!.Value)
            .Distinct()
            .ToList();

        var userMap = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        return Ok(rows.Select(x => new UnitListItemDto(
            x.Id,
            x.Code,
            x.Name,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Query)]
    public async Task<ActionResult<UnitListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Units
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (row is null)
            return NotFound();

        var userName = row.UpdateBy is null
            ? null
            : await db.Users.AsNoTracking()
                .Where(u => u.Id == row.UpdateBy.Value)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct);

        return Ok(new UnitListItemDto(row.Id, row.Code, row.Name, row.IsActive, row.UpdateTime, row.UpdateBy, userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Create)]
    public async Task<ActionResult<UnitListItemDto>> Create([FromBody] UpsertUnitDto dto, CancellationToken ct)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest("单位编号和单位名称不能为空。");

        var exists = await db.Units
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);
        if (exists)
            return Conflict("单位编号已存在。");

        var entity = new Unit
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Code = code,
            Name = name,
            IsActive = dto.IsActive
        };
        db.Units.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new UnitListItemDto(entity.Id, entity.Code, entity.Name, entity.IsActive, entity.UpdateTime, entity.UpdateBy, null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertUnitDto dto, CancellationToken ct)
    {
        var entity = await db.Units
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest("单位编号和单位名称不能为空。");

        var codeUsed = await db.Units
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);
        if (codeUsed)
            return Conflict("单位编号已存在。");

        entity.Code = code;
        entity.Name = name;
        entity.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Units
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.Units.Remove(entity);
        try
        {
            await db.SaveChangesAsync(ct);
            return NoContent();
        }
        catch (DbUpdateException)
        {
            db.Entry(entity).State = EntityState.Unchanged;
            entity.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Ok("单位存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Units.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<UnitImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }
}
