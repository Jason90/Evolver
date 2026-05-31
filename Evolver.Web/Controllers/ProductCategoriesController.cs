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
[Route("api/product-categories")]
[Authorize]
public sealed class ProductCategoriesController(
    AppDbContext db,
    ITenantContext tenant,
    ProductCategorySpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet("tree")]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Query)]
    public async Task<ActionResult<IReadOnlyList<ProductCategoryTreeNodeDto>>> GetTree(CancellationToken ct)
    {
        var rows = await db.ProductCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.ParentId)
            .ThenBy(x => x.Code)
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

        var nodes = rows.ToDictionary(
            x => x.Id,
            x => new ProductCategoryTreeNodeDto(
                x.Id,
                x.ParentId,
                x.Code,
                x.Name,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy,
                x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value),
                new List<ProductCategoryTreeNodeDto>()));

        var roots = new List<ProductCategoryTreeNodeDto>();
        foreach (var n in nodes.Values)
        {
            if (n.ParentId is null || !nodes.TryGetValue(n.ParentId.Value, out var parent))
                roots.Add(n);
            else
                parent.Children.Add(n);
        }

        return Ok(roots);
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Query)]
    public async Task<ActionResult<ProductCategoryDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.ProductCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (row is null)
            return NotFound();

        string? updateByName = null;
        if (row.UpdateBy is { } updateBy)
        {
            updateByName = await db.Users.AsNoTracking()
                .Where(u => u.Id == updateBy)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct);
        }

        return Ok(new ProductCategoryDto(
            row.Id, row.ParentId, row.Code, row.Name, row.IsActive, row.UpdateTime, row.UpdateBy, updateByName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Create)]
    public async Task<ActionResult<ProductCategoryDto>> Create([FromBody] CreateProductCategoryDto dto, CancellationToken ct)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest("类别编号和类别名称不能为空。");

        if (dto.ParentId is { } pid &&
            !await db.ProductCategories.IgnoreQueryFilters().AnyAsync(x => x.Id == pid && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct))
            return BadRequest("父级类别不存在。");

        var exists = await db.ProductCategories.IgnoreQueryFilters()
            .AnyAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);
        if (exists)
            return Conflict("类别编号已存在。");

        var entity = new ProductCategory
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            ParentId = dto.ParentId,
            Code = code,
            Name = name,
            IsActive = dto.IsActive
        };

        db.ProductCategories.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(new ProductCategoryDto(
            entity.Id, entity.ParentId, entity.Code, entity.Name, entity.IsActive, entity.UpdateTime, entity.UpdateBy, null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateProductCategoryDto dto, CancellationToken ct)
    {
        var entity = await db.ProductCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return BadRequest("类别编号和类别名称不能为空。");

        if (dto.ParentId == id)
            return BadRequest("父级不能是自身。");

        if (dto.ParentId is { } pid)
        {
            if (!await db.ProductCategories.IgnoreQueryFilters().AnyAsync(x => x.Id == pid && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct))
                return BadRequest("父级类别不存在。");
            if (await IsInSubtreeAsync(id, pid, ct))
                return BadRequest("不能将父级设置为当前节点的子节点。");
        }

        var codeUsed = await db.ProductCategories.IgnoreQueryFilters()
            .AnyAsync(x => x.Id != id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);
        if (codeUsed)
            return Conflict("类别编号已存在。");

        entity.Code = code;
        entity.Name = name;
        entity.ParentId = dto.ParentId;
        entity.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.ProductCategories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.ProductCategories.Remove(entity);
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
            return Ok("类别存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.ProductCategories.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ProductCategoryImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, tenant.TenantId, tenant.OrgId, ct);
        return Ok(result);
    }

    private async Task<bool> IsInSubtreeAsync(long rootId, long candidateAncestorId, CancellationToken ct)
    {
        var current = candidateAncestorId;
        var guard = 0;
        while (guard++ < 256)
        {
            if (current == rootId)
                return true;
            var parentId = await db.ProductCategories
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(x => x.Id == current && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
                .Select(x => x.ParentId)
                .FirstOrDefaultAsync(ct);
            if (parentId is null)
                return false;
            current = parentId.Value;
        }

        return true;
    }
}
