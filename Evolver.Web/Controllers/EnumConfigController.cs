using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/enum-config")]
[Authorize]
public sealed class EnumConfigController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("types")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Query)]
    public async Task<ActionResult<IReadOnlyList<EnumTypeDto>>> ListTypes(CancellationToken ct)
    {
        var rows = await db.EnumTypeConfigs.AsNoTracking()
            .OrderBy(x => x.EnumTypeCode)
            .Select(x => new EnumTypeDto(x.EnumTypeCode, x.Name, x.Description, x.IsActive, x.UpdateTime))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("types")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Create)]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Update)]
    public async Task<ActionResult<EnumTypeDto>> UpsertType([FromBody] UpsertEnumTypeDto dto, CancellationToken ct)
    {
        var typeCode = dto.EnumTypeCode.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(name))
            return BadRequest("EnumTypeCode 与 Name 不能为空。");

        var row = await db.EnumTypeConfigs.FirstOrDefaultAsync(x => x.EnumTypeCode == typeCode, ct);
        if (row is null)
        {
            row = new EnumTypeConfig
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                EnumTypeCode = typeCode,
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive,
            };
            db.EnumTypeConfigs.Add(row);
        }
        else
        {
            row.Name = name;
            row.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            row.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new EnumTypeDto(row.EnumTypeCode, row.Name, row.Description, row.IsActive, row.UpdateTime));
    }

    [HttpDelete("types/{enumTypeCode}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Delete)]
    public async Task<ActionResult> DeleteType(string enumTypeCode, CancellationToken ct)
    {
        var code = enumTypeCode.Trim();
        var row = await db.EnumTypeConfigs.FirstOrDefaultAsync(x => x.EnumTypeCode == code, ct);
        if (row is null)
            return NotFound();

        var hasValues = await db.EnumValueConfigs.AnyAsync(x => x.EnumTypeCode == code, ct);
        if (hasValues)
            return BadRequest("该枚举类型下仍有值，请先删除枚举值。");

        db.EnumTypeConfigs.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("values")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Query)]
    public async Task<ActionResult<IReadOnlyList<EnumValueDto>>> ListValues([FromQuery] string enumTypeCode, CancellationToken ct)
    {
        var code = enumTypeCode.Trim();
        var rows = await db.EnumValueConfigs.AsNoTracking()
            .Where(x => x.EnumTypeCode == code)
            .OrderBy(x => x.SortNo)
            .ThenBy(x => x.EnumValueCode)
            .Select(x => new EnumValueDto(x.EnumTypeCode, x.EnumValueCode, x.Name, x.SortNo, x.IsDefault, x.Description, x.IsActive, x.UpdateTime))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("values")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Create)]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Update)]
    public async Task<ActionResult<EnumValueDto>> UpsertValue([FromBody] UpsertEnumValueDto dto, CancellationToken ct)
    {
        var typeCode = dto.EnumTypeCode.Trim();
        var valueCode = dto.EnumValueCode.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(typeCode) || string.IsNullOrWhiteSpace(valueCode) || string.IsNullOrWhiteSpace(name))
            return BadRequest("EnumTypeCode / EnumValueCode / Name 不能为空。");

        var typeExists = await db.EnumTypeConfigs.AnyAsync(x => x.EnumTypeCode == typeCode, ct);
        if (!typeExists)
            return BadRequest("枚举类型不存在。");

        var row = await db.EnumValueConfigs.FirstOrDefaultAsync(x => x.EnumTypeCode == typeCode && x.EnumValueCode == valueCode, ct);
        if (row is null)
        {
            row = new EnumValueConfig
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                EnumTypeCode = typeCode,
                EnumValueCode = valueCode,
                Name = name,
                SortNo = dto.SortNo,
                IsDefault = dto.IsDefault,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                IsActive = dto.IsActive,
            };
            db.EnumValueConfigs.Add(row);
        }
        else
        {
            row.Name = name;
            row.SortNo = dto.SortNo;
            row.IsDefault = dto.IsDefault;
            row.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            row.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync(ct);
        return Ok(new EnumValueDto(row.EnumTypeCode, row.EnumValueCode, row.Name, row.SortNo, row.IsDefault, row.Description, row.IsActive, row.UpdateTime));
    }

    [HttpDelete("values/{enumTypeCode}/{enumValueCode}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.EnumConfig.Delete)]
    public async Task<ActionResult> DeleteValue(string enumTypeCode, string enumValueCode, CancellationToken ct)
    {
        var typeCode = enumTypeCode.Trim();
        var valueCode = enumValueCode.Trim();
        var row = await db.EnumValueConfigs.FirstOrDefaultAsync(x => x.EnumTypeCode == typeCode && x.EnumValueCode == valueCode, ct);
        if (row is null)
            return NotFound();

        db.EnumValueConfigs.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

