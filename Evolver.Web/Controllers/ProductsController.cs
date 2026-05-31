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
[Route("api/products")]
[Authorize]
public sealed class ProductsController(AppDbContext db, ITenantContext tenant, ProductSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Query)]
    public async Task<ActionResult<IReadOnlyList<ProductListItemDto>>> Get(CancellationToken ct)
    {
        var categoryMap = await db.ProductCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);
        var unitMap = await db.Units
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);

        var rows = await db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(x => x.Code)
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

        return Ok(rows.Select(x => new ProductListItemDto(
            x.Id,
            x.Code,
            x.Name,
            x.ProductCategoryId is null ? null : categoryMap.GetValueOrDefault(x.ProductCategoryId.Value),
            x.UnitId is null ? null : unitMap.GetValueOrDefault(x.UnitId.Value),
            x.Barcode,
            x.Brand,
            x.Model,
            x.UnitCost,
            x.SuggestedPrice,
            x.TheoreticalStock,
            x.ActualStock,
            x.AlertStock,
            x.Remark,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Query)]
    public async Task<ActionResult<ProductListItemDto>> GetById(long id, CancellationToken ct)
    {
        var categoryMap = await db.ProductCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);
        var unitMap = await db.Units
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);

        var row = await db.Products
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .FirstOrDefaultAsync(ct);

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

        return Ok(new ProductListItemDto(
            row.Id,
            row.Code,
            row.Name,
            row.ProductCategoryId is null ? null : categoryMap.GetValueOrDefault(row.ProductCategoryId.Value),
            row.UnitId is null ? null : unitMap.GetValueOrDefault(row.UnitId.Value),
            row.Barcode,
            row.Brand,
            row.Model,
            row.UnitCost,
            row.SuggestedPrice,
            row.TheoreticalStock,
            row.ActualStock,
            row.AlertStock,
            row.Remark,
            row.IsActive,
            row.UpdateTime,
            row.UpdateBy,
            updateByName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Create)]
    public async Task<ActionResult<ProductListItemDto>> Create([FromBody] UpsertProductDto dto, CancellationToken ct)
    {
        var validate = await ValidateAndResolveAsync(dto, null, ct);
        if (!validate.Ok)
            return BadRequest(validate.Error);

        var entity = new Product
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Code = validate.Code!,
            Name = validate.Name!,
            ProductCategoryId = validate.ProductCategoryId,
            UnitId = validate.UnitId,
            Barcode = validate.Barcode,
            Brand = validate.Brand,
            Model = validate.Model,
            UnitCost = dto.UnitCost,
            SuggestedPrice = dto.SuggestedPrice,
            UnitPrice = dto.SuggestedPrice ?? 0m,
            TheoreticalStock = dto.TheoreticalStock,
            ActualStock = dto.ActualStock,
            AlertStock = dto.AlertStock,
            Remark = validate.Remark,
            IsActive = dto.IsActive
        };

        db.Products.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("商品代码已存在。");
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new ProductListItemDto(
                entity.Id,
                entity.Code,
                entity.Name,
                dto.ProductTypeCode,
                dto.UnitCode,
                entity.Barcode,
                entity.Brand,
                entity.Model,
                entity.UnitCost,
                entity.SuggestedPrice,
                entity.TheoreticalStock,
                entity.ActualStock,
                entity.AlertStock,
                entity.Remark,
                entity.IsActive,
                entity.UpdateTime,
                entity.UpdateBy,
                null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertProductDto dto, CancellationToken ct)
    {
        var entity = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var validate = await ValidateAndResolveAsync(dto, id, ct);
        if (!validate.Ok)
            return BadRequest(validate.Error);

        entity.Code = validate.Code!;
        entity.Name = validate.Name!;
        entity.ProductCategoryId = validate.ProductCategoryId;
        entity.UnitId = validate.UnitId;
        entity.Barcode = validate.Barcode;
        entity.Brand = validate.Brand;
        entity.Model = validate.Model;
        entity.UnitCost = dto.UnitCost;
        entity.SuggestedPrice = dto.SuggestedPrice;
        entity.UnitPrice = dto.SuggestedPrice ?? 0m;
        entity.TheoreticalStock = dto.TheoreticalStock;
        entity.ActualStock = dto.ActualStock;
        entity.AlertStock = dto.AlertStock;
        entity.Remark = validate.Remark;
        entity.IsActive = dto.IsActive;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("商品代码已存在。");
        }

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.Products.Remove(entity);
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
            return Ok("商品存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Products.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<ProductImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    private async Task<(bool Ok, string? Error, string? Code, string? Name, long? ProductCategoryId, long? UnitId, string? Barcode, string? Brand, string? Model, string? Remark)>
        ValidateAndResolveAsync(UpsertProductDto dto, long? id, CancellationToken ct)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            return (false, "商品代码和商品名称不能为空。", null, null, null, null, null, null, null, null);

        var codeExists = await db.Products
            .IgnoreQueryFilters()
            .AnyAsync(x =>
                x.TenantId == tenant.TenantId &&
                x.OrgId == tenant.OrgId &&
                x.Code == code &&
                (!id.HasValue || x.Id != id.Value), ct);
        if (codeExists)
            return (false, "商品代码已存在。", null, null, null, null, null, null, null, null);

        long? categoryId = null;
        if (!string.IsNullOrWhiteSpace(dto.ProductTypeCode))
        {
            var categoryCode = dto.ProductTypeCode.Trim();

            var enumValue = await db.EnumValueConfigs
                .IgnoreQueryFilters()
                .Where(x =>
                    x.TenantId == tenant.TenantId &&
                    x.OrgId == tenant.OrgId &&
                    x.EnumTypeCode == "ProductType" &&
                    x.EnumValueCode == categoryCode &&
                    x.IsActive)
                .FirstOrDefaultAsync(ct);
            if (enumValue is null)
                return (false, $"商品类型不存在：{categoryCode}", null, null, null, null, null, null, null, null);

            var category = await db.ProductCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x =>
                    x.TenantId == tenant.TenantId &&
                    x.OrgId == tenant.OrgId &&
                    x.Code == categoryCode, ct);
            if (category is null)
            {
                category = new ProductCategory
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Code = enumValue.EnumValueCode,
                    Name = enumValue.Name,
                    IsActive = true
                };
                db.ProductCategories.Add(category);
                await db.SaveChangesAsync(ct);
            }
            else if (!string.Equals(category.Name, enumValue.Name, StringComparison.Ordinal))
            {
                category.Name = enumValue.Name;
                if (!category.IsActive)
                    category.IsActive = true;
                await db.SaveChangesAsync(ct);
            }
            categoryId = category.Id;
        }

        long? unitId = null;
        if (!string.IsNullOrWhiteSpace(dto.UnitCode))
        {
            var unitCode = dto.UnitCode.Trim();
            unitId = await db.Units
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == unitCode)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(ct);
            if (unitId is null)
                return (false, $"单位编号不存在：{unitCode}", null, null, null, null, null, null, null, null);
        }

        return (true, null, code, name, categoryId, unitId,
            string.IsNullOrWhiteSpace(dto.Barcode) ? null : dto.Barcode.Trim(),
            string.IsNullOrWhiteSpace(dto.Brand) ? null : dto.Brand.Trim(),
            string.IsNullOrWhiteSpace(dto.Model) ? null : dto.Model.Trim(),
            string.IsNullOrWhiteSpace(dto.Remark) ? null : dto.Remark.Trim());
    }
}
