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
[Route("api/suppliers")]
[Authorize]
public sealed class SuppliersController(AppDbContext db, ITenantContext tenant, SupplierSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Query)]
    public async Task<ActionResult<IReadOnlyList<SupplierListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.Suppliers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Address,
                x.Phone,
                x.Website,
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

        return Ok(rows.Select(x => new SupplierListItemDto(
            x.Id,
            x.Name,
            x.Address,
            x.Phone,
            x.Website,
            x.Remark,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Query)]
    public async Task<ActionResult<SupplierListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Suppliers
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

        return Ok(new SupplierListItemDto(
            row.Id,
            row.Name,
            row.Address,
            row.Phone,
            row.Website,
            row.Remark,
            row.IsActive,
            row.UpdateTime,
            row.UpdateBy,
            userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Create)]
    public async Task<ActionResult<SupplierListItemDto>> Create([FromBody] UpsertSupplierDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("供应商名称不能为空。");

        var exists = await db.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.TenantId == tenant.TenantId
                     && x.OrgId == tenant.OrgId
                     && x.Name.ToUpper() == name.ToUpper(),
                ct);
        if (exists)
            return Conflict("供应商名称已存在。");

        var entity = new Supplier
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Code = $"SUP-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Name = name,
            Address = Normalize(dto.Address),
            Phone = Normalize(dto.Phone),
            Website = Normalize(dto.Website),
            Remark = Normalize(dto.Remark),
            IsActive = dto.IsActive
        };
        db.Suppliers.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new SupplierListItemDto(
            entity.Id,
            entity.Name,
            entity.Address,
            entity.Phone,
            entity.Website,
            entity.Remark,
            entity.IsActive,
            entity.UpdateTime,
            entity.UpdateBy,
            null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertSupplierDto dto, CancellationToken ct)
    {
        var entity = await db.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("供应商名称不能为空。");

        var used = await db.Suppliers
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Id != id
                     && x.TenantId == tenant.TenantId
                     && x.OrgId == tenant.OrgId
                     && x.Name.ToUpper() == name.ToUpper(),
                ct);
        if (used)
            return Conflict("供应商名称已存在。");

        entity.Name = name;
        entity.Address = Normalize(dto.Address);
        entity.Phone = Normalize(dto.Phone);
        entity.Website = Normalize(dto.Website);
        entity.Remark = Normalize(dto.Remark);
        entity.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Suppliers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.Suppliers.Remove(entity);
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
            return Ok("供应商存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<SupplierImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("active")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Suppliers.Query)]
    public async Task<ActionResult<IReadOnlyList<SupplierListItemDto>>> ListActive(CancellationToken ct)
    {
        var rows = await db.Suppliers.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SupplierListItemDto(
                x.Id,
                x.Name,
                x.Address,
                x.Phone,
                x.Website,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy,
                null))
            .ToListAsync(ct);

        return Ok(rows);
    }

    private static string? Normalize(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }
}
