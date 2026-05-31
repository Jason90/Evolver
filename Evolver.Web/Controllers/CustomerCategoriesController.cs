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
[Route("api/customer-categories")]
[Authorize]
public sealed class CustomerCategoriesController(AppDbContext db, ITenantContext tenant, CustomerCategorySpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Query)]
    public async Task<ActionResult<IReadOnlyList<CustomerCategoryListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.CustomerCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.CategoryCode)
            .Select(x => new
            {
                x.Id,
                x.CategoryCode,
                x.Name,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .ToListAsync(ct);

        var userIds = rows.Where(x => x.UpdateBy is not null).Select(x => (long)x.UpdateBy!.Value).Distinct().ToList();
        var userMap = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        return Ok(rows.Select(x => new CustomerCategoryListItemDto(
            x.Id, x.CategoryCode, x.Name, x.Remark, x.IsActive, x.UpdateTime, x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Query)]
    public async Task<ActionResult<CustomerCategoryListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.CustomerCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (row is null) return NotFound();

        var userName = row.UpdateBy is null
            ? null
            : await db.Users.AsNoTracking().Where(u => u.Id == row.UpdateBy.Value).Select(u => u.UserName).FirstOrDefaultAsync(ct);
        return Ok(new CustomerCategoryListItemDto(row.Id, row.CategoryCode, row.Name, row.Remark, row.IsActive, row.UpdateTime, row.UpdateBy, userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Create)]
    public async Task<ActionResult<CustomerCategoryListItemDto>> Create([FromBody] UpsertCustomerCategoryDto dto, CancellationToken ct)
    {
        var categoryCode = dto.CategoryCode.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(categoryCode) || string.IsNullOrWhiteSpace(name))
            return BadRequest("类别代码和类别名称不能为空。");

        var exists = await db.CustomerCategories.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.CategoryCode == categoryCode, ct);
        if (exists) return Conflict("类别代码已存在。");

        var entity = new CustomerCategory
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            CategoryCode = categoryCode,
            Name = name,
            Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim(),
            IsActive = dto.IsActive
        };
        db.CustomerCategories.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new CustomerCategoryListItemDto(entity.Id, entity.CategoryCode, entity.Name, entity.Remark, entity.IsActive, entity.UpdateTime, entity.UpdateBy, null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertCustomerCategoryDto dto, CancellationToken ct)
    {
        var entity = await db.CustomerCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null) return NotFound();

        var categoryCode = dto.CategoryCode.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(categoryCode) || string.IsNullOrWhiteSpace(name))
            return BadRequest("类别代码和类别名称不能为空。");

        var used = await db.CustomerCategories.IgnoreQueryFilters()
            .AnyAsync(x => x.Id != id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.CategoryCode == categoryCode, ct);
        if (used) return Conflict("类别代码已存在。");

        entity.CategoryCode = categoryCode;
        entity.Name = name;
        entity.Remark = string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim();
        entity.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.CustomerCategories.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null) return NotFound();

        db.CustomerCategories.Remove(entity);
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
            return Ok("客户类别存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<CustomerCategoryImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)) return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("active")]
    [RequirePermission(NavSystemSettingsPermissionCodes.CustomerCategories.Query)]
    public async Task<ActionResult<IReadOnlyList<CustomerCategoryListItemDto>>> ListActive(CancellationToken ct)
    {
        var rows = await db.CustomerCategories.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.CategoryCode)
            .Select(x => new CustomerCategoryListItemDto(
                x.Id,
                x.CategoryCode,
                x.Name,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy,
                null))
            .ToListAsync(ct);

        return Ok(rows);
    }
}
