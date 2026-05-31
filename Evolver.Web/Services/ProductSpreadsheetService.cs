using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class ProductSpreadsheetService(AppDbContext db, ITenantContext tenant)
{
    private const string SheetName = "Products";

    public async Task<ProductImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new ProductImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var map = MapHeaders(range.FirstRow());
        if (!map.TryGetValue("code", out var cCode) || !map.TryGetValue("name", out var cName))
            return new ProductImportResultDto(0, 0, 0, new[] { "未找到商品代码或商品名称列。" });

        map.TryGetValue("categorycode", out var cCategoryCode);
        map.TryGetValue("unitcode", out var cUnitCode);
        map.TryGetValue("barcode", out var cBarcode);
        map.TryGetValue("brand", out var cBrand);
        map.TryGetValue("model", out var cModel);
        map.TryGetValue("unitcost", out var cUnitCost);
        map.TryGetValue("suggestedprice", out var cSuggestedPrice);
        map.TryGetValue("theoreticalstock", out var cTheoreticalStock);
        map.TryGetValue("actualstock", out var cActualStock);
        map.TryGetValue("alertstock", out var cAlertStock);
        map.TryGetValue("remark", out var cRemark);
        map.TryGetValue("active", out var cActive);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var messages = new List<string>();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var code = row.Cell(cCode).GetString().Trim();
            var name = row.Cell(cName).GetString().Trim();
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            var categoryCode = cCategoryCode > 0 ? row.Cell(cCategoryCode).GetString().Trim() : "";
            var unitCode = cUnitCode > 0 ? row.Cell(cUnitCode).GetString().Trim() : "";

            long? productCategoryId = null;
            if (!string.IsNullOrWhiteSpace(categoryCode))
            {
                productCategoryId = await db.ProductCategories
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == categoryCode)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync(ct);
                if (productCategoryId is null)
                {
                    messages.Add($"第 {row.RowNumber()} 行商品类型编号不存在：{categoryCode}");
                    skipped++;
                    continue;
                }
            }

            long? unitId = null;
            if (!string.IsNullOrWhiteSpace(unitCode))
            {
                unitId = await db.Units
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == unitCode)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync(ct);
                if (unitId is null)
                {
                    messages.Add($"第 {row.RowNumber()} 行单位编号不存在：{unitCode}");
                    skipped++;
                    continue;
                }
            }

            var unitCost = cUnitCost > 0 ? ParseNullableDecimal(row.Cell(cUnitCost).GetString()) : null;
            var suggestedPrice = cSuggestedPrice > 0 ? ParseNullableDecimal(row.Cell(cSuggestedPrice).GetString()) : null;
            var theoreticalStock = cTheoreticalStock > 0 ? ParseDecimalOrZero(row.Cell(cTheoreticalStock).GetString()) : 0m;
            var actualStock = cActualStock > 0 ? ParseDecimalOrZero(row.Cell(cActualStock).GetString()) : 0m;
            var alertStock = cAlertStock > 0 ? ParseDecimalOrZero(row.Cell(cAlertStock).GetString()) : 0m;
            var isActive = cActive > 0 ? ParseActiveStatus(row.Cell(cActive).GetString()) : true;

            var existing = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);
            if (existing is null)
            {
                db.Products.Add(new Product
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Code = code,
                    Name = name,
                    ProductCategoryId = productCategoryId,
                    UnitId = unitId,
                    Barcode = cBarcode > 0 ? NullIfEmpty(row.Cell(cBarcode).GetString()) : null,
                    Brand = cBrand > 0 ? NullIfEmpty(row.Cell(cBrand).GetString()) : null,
                    Model = cModel > 0 ? NullIfEmpty(row.Cell(cModel).GetString()) : null,
                    UnitCost = unitCost,
                    SuggestedPrice = suggestedPrice,
                    UnitPrice = suggestedPrice ?? 0m,
                    TheoreticalStock = theoreticalStock,
                    ActualStock = actualStock,
                    AlertStock = alertStock,
                    Remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString()) : null,
                    IsActive = isActive
                });
                created++;
            }
            else
            {
                existing.Name = name;
                existing.ProductCategoryId = productCategoryId;
                existing.UnitId = unitId;
                existing.Barcode = cBarcode > 0 ? NullIfEmpty(row.Cell(cBarcode).GetString()) : null;
                existing.Brand = cBrand > 0 ? NullIfEmpty(row.Cell(cBrand).GetString()) : null;
                existing.Model = cModel > 0 ? NullIfEmpty(row.Cell(cModel).GetString()) : null;
                existing.UnitCost = unitCost;
                existing.SuggestedPrice = suggestedPrice;
                existing.UnitPrice = suggestedPrice ?? 0m;
                existing.TheoreticalStock = theoreticalStock;
                existing.ActualStock = actualStock;
                existing.AlertStock = alertStock;
                existing.Remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString()) : null;
                existing.IsActive = isActive;
                updated++;
            }
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            messages.Add($"导入保存失败：{ex.GetBaseException().Message}");
        }

        return new ProductImportResultDto(created, updated, skipped, messages);
    }

    private static Dictionary<string, int> MapHeaders(IXLRangeRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrEmpty(key))
                map[key] = cell.Address.ColumnNumber;
        }
        return map;
    }

    private static string NormalizeHeader(string raw)
    {
        var t = raw.Trim();
        return t.ToLowerInvariant() switch
        {
            "商品代码" or "code" or "productcode" => "code",
            "商品名称" or "name" or "productname" => "name",
            "商品类型编号" or "producttypecode" or "categorycode" => "categorycode",
            "单位编号" or "unitcode" => "unitcode",
            "商品条码" or "barcode" => "barcode",
            "品牌" or "brand" => "brand",
            "型号" or "model" => "model",
            "成本单价" or "unitcost" => "unitcost",
            "建议售价" or "suggestedprice" => "suggestedprice",
            "理论库存" or "theoreticalstock" => "theoreticalstock",
            "实际库存" or "actualstock" => "actualstock",
            "警戒库存" or "alertstock" => "alertstock",
            "备注" or "remark" => "remark",
            "是否激活" or "状态" or "isactive" or "active" => "active",
            _ => t.ToLowerInvariant()
        };
    }

    private static decimal ParseDecimalOrZero(string? raw)
        => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

    private static decimal? ParseNullableDecimal(string? raw)
        => decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static bool ParseActiveStatus(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return true;
        var t = s.Trim();
        if (bool.TryParse(t, out var b))
            return b;
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n != 0;
        return t switch
        {
            "正常" or "启用" or "active" or "y" or "yes" => true,
            "停用" or "禁用" or "inactive" or "disabled" or "n" or "no" => false,
            _ => true
        };
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
