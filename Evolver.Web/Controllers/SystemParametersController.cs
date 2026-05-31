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
[Route("api/system-parameters")]
[Authorize]
public sealed class SystemParametersController(AppDbContext db, ITenantContext tenant, SystemParameterSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Query)]
    public async Task<ActionResult<IReadOnlyList<SystemParameterListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.SystemParameters
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.ParamKey)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.ParamKey,
                x.ParamValue,
                x.IsSystemBuiltIn,
                x.Remark,
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

        return Ok(rows.Select(x => new SystemParameterListItemDto(
            x.Id,
            x.Name,
            x.ParamKey,
            x.ParamValue,
            x.IsSystemBuiltIn,
            x.Remark,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Query)]
    public async Task<ActionResult<SystemParameterListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.SystemParameters
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

        return Ok(new SystemParameterListItemDto(
            row.Id,
            row.Name,
            row.ParamKey,
            row.ParamValue,
            row.IsSystemBuiltIn,
            row.Remark,
            row.IsActive,
            row.UpdateTime,
            row.UpdateBy,
            userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Create)]
    public async Task<ActionResult<SystemParameterListItemDto>> Create([FromBody] UpsertSystemParameterDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        var key = dto.ParamKey.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
            return BadRequest("参数名称和参数键名不能为空。");

        var keyUsed = await db.SystemParameters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.ParamKey == key, ct);
        if (keyUsed)
            return Conflict("参数键名已存在。");

        var entity = new SystemParameter
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Name = name,
            ParamKey = key,
            ParamValue = dto.ParamValue ?? "",
            IsSystemBuiltIn = dto.IsSystemBuiltIn,
            Remark = Normalize(dto.Remark),
            IsActive = dto.IsActive
        };
        db.SystemParameters.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(new SystemParameterListItemDto(
            entity.Id,
            entity.Name,
            entity.ParamKey,
            entity.ParamValue,
            entity.IsSystemBuiltIn,
            entity.Remark,
            entity.IsActive,
            entity.UpdateTime,
            entity.UpdateBy,
            null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertSystemParameterDto dto, CancellationToken ct)
    {
        var entity = await db.SystemParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var name = dto.Name.Trim();
        var key = dto.ParamKey.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
            return BadRequest("参数名称和参数键名不能为空。");

        var keyUsed = await db.SystemParameters
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Id != id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.ParamKey == key, ct);
        if (keyUsed)
            return Conflict("参数键名已存在。");

        entity.Name = name;
        entity.ParamKey = key;
        entity.ParamValue = dto.ParamValue ?? "";
        entity.IsSystemBuiltIn = dto.IsSystemBuiltIn;
        entity.Remark = Normalize(dto.Remark);
        entity.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.SystemParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.SystemParameters.Remove(entity);
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
            return Ok("参数存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Parameters.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<SystemParameterImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    private static string? Normalize(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }
}
