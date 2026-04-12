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
[Route("api/data-dictionary")]
[Authorize]
public sealed class DataDictionaryController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("types")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Query)]
    public async Task<ActionResult<IReadOnlyList<DataDictionaryTypeDto>>> ListTypes(
        [FromQuery] string? name,
        [FromQuery] string? code,
        [FromQuery] bool? active,
        CancellationToken ct)
    {
        var q = db.DataDictionaryTypes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.Trim();
            q = q.Where(x => x.TypeName.Contains(n));
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var c = code.Trim();
            q = q.Where(x => x.TypeCode.Contains(c));
        }

        if (active is not null)
            q = q.Where(x => x.IsActive == active.Value);

        var rows = await q
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.TypeCode)
            .Select(x => new DataDictionaryTypeDto(
                x.Id,
                x.TypeCode,
                x.TypeName,
                x.Remark,
                x.IsActive,
                x.SortOrder,
                x.UpdateTime))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost("types")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Create)]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Update)]
    public async Task<ActionResult<DataDictionaryTypeDto>> UpsertType([FromBody] UpsertDataDictionaryTypeDto dto, CancellationToken ct)
    {
        var typeCode = dto.TypeCode.Trim();
        var typeName = dto.TypeName.Trim();
        if (string.IsNullOrEmpty(typeCode) || string.IsNullOrEmpty(typeName))
            return BadRequest("TypeCode 与 TypeName 不能为空。");

        var existing = await db.DataDictionaryTypes.FirstOrDefaultAsync(x => x.TypeCode == typeCode, ct);
        if (existing is null)
        {
            existing = new DataDictionaryType
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                TypeCode = typeCode,
                TypeName = typeName,
                Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim(),
                IsActive = dto.IsActive,
                SortOrder = dto.SortOrder
            };
            db.DataDictionaryTypes.Add(existing);
        }
        else
        {
            existing.TypeName = typeName;
            existing.Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim();
            existing.IsActive = dto.IsActive;
            existing.SortOrder = dto.SortOrder;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new DataDictionaryTypeDto(
            existing.Id,
            existing.TypeCode,
            existing.TypeName,
            existing.Remark,
            existing.IsActive,
            existing.SortOrder,
            existing.UpdateTime));
    }

    [HttpDelete("types/{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Delete)]
    public async Task<ActionResult> DeleteType(long id, CancellationToken ct)
    {
        var row = await db.DataDictionaryTypes.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return NotFound();

        var hasItems = await db.DataDictionaryItems.AnyAsync(x => x.CategoryCode == row.TypeCode, ct);
        if (hasItems)
            return BadRequest("该字典类型下仍有字典数据，请先删除字典项。");

        db.DataDictionaryTypes.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("items")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Query)]
    public async Task<ActionResult<IReadOnlyList<DataDictionaryItemDto>>> ListItems([FromQuery] string? category, CancellationToken ct)
    {
        var q = db.DataDictionaryItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(x => x.CategoryCode == category.Trim());

        var raw = await q
            .OrderBy(x => x.CategoryCode)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.ItemCode)
            .Select(x => new
            {
                x.Id,
                x.CategoryCode,
                x.ItemCode,
                x.DisplayName,
                x.ItemValue,
                x.Remark,
                x.SortOrder,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .ToListAsync(ct);

        var userIds = raw
            .Select(r => r.UpdateBy)
            .Where(x => x is not null)
            .Select(x => (long)x!.Value)
            .Distinct()
            .ToList();
        var nameMap = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        var rows = raw.Select(x => new DataDictionaryItemDto(
            x.Id,
            x.CategoryCode,
            x.ItemCode,
            x.DisplayName,
            x.ItemValue,
            x.Remark,
            x.SortOrder,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : nameMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList();

        return Ok(rows);
    }

    [HttpGet("items/{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Query)]
    public async Task<ActionResult<DataDictionaryItemDto>> GetItem(long id, CancellationToken ct)
    {
        var x = await db.DataDictionaryItems.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (x is null)
            return NotFound();

        var un = await ResolveUserNameAsync(x.UpdateBy, ct);
        return Ok(new DataDictionaryItemDto(
            x.Id,
            x.CategoryCode,
            x.ItemCode,
            x.DisplayName,
            x.ItemValue,
            x.Remark,
            x.SortOrder,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            un));
    }

    [HttpPost("items")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Create)]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Update)]
    public async Task<ActionResult<DataDictionaryItemDto>> UpsertItem([FromBody] UpsertDataDictionaryItemDto dto, CancellationToken ct)
    {
        var cat = dto.CategoryCode.Trim();
        var code = dto.ItemCode.Trim();
        var name = dto.DisplayName.Trim();
        if (string.IsNullOrEmpty(cat) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return BadRequest("CategoryCode, ItemCode, DisplayName are required.");

        var existing = await db.DataDictionaryItems.FirstOrDefaultAsync(
            x => x.CategoryCode == cat && x.ItemCode == code, ct);

        if (existing is null)
        {
            existing = new DataDictionaryItem
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                CategoryCode = cat,
                ItemCode = code,
                DisplayName = name,
                ItemValue = string.IsNullOrWhiteSpace(dto.ItemValue) ? null : dto.ItemValue.Trim(),
                Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive
            };
            db.DataDictionaryItems.Add(existing);
        }
        else
        {
            existing.DisplayName = name;
            existing.ItemValue = string.IsNullOrWhiteSpace(dto.ItemValue) ? null : dto.ItemValue.Trim();
            existing.Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim();
            existing.SortOrder = dto.SortOrder;
            existing.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync(ct);

        var un = await ResolveUserNameAsync(existing.UpdateBy, ct);
        return Ok(new DataDictionaryItemDto(
            existing.Id,
            existing.CategoryCode,
            existing.ItemCode,
            existing.DisplayName,
            existing.ItemValue,
            existing.Remark,
            existing.SortOrder,
            existing.IsActive,
            existing.UpdateTime,
            existing.UpdateBy,
            un));
    }

    [HttpDelete("items/{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Dictionary.Delete)]
    public async Task<ActionResult> DeleteItem(long id, CancellationToken ct)
    {
        var row = await db.DataDictionaryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return NotFound();

        db.DataDictionaryItems.Remove(row);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string?> ResolveUserNameAsync(int? updateBy, CancellationToken ct)
    {
        if (updateBy is null)
            return null;
        var id = (long)updateBy.Value;
        return await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(ct);
    }
}
