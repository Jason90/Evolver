using System.Globalization;
using ClosedXML.Excel;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class ProductCategorySpreadsheetService(AppDbContext db)
{
    private const string SheetName = "ProductCategories";

    public async Task<ProductCategoryImportResultDto> ImportAsync(Stream xlsxStream, int tenantId, int orgId, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new ProductCategoryImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerMap = MapHeaders(range.FirstRow());
        if (!headerMap.TryGetValue("code", out var cCode) || !headerMap.TryGetValue("name", out var cName))
            return new ProductCategoryImportResultDto(0, 0, 0, new[] { "未找到类别编号或类别名称列。" });

        headerMap.TryGetValue("parentcode", out var cParentCode);
        headerMap.TryGetValue("active", out var cActive);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var messages = new List<string>();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var code = row.Cell(cCode).GetString().Trim();
            var name = row.Cell(cName).GetString().Trim();
            var parentCode = cParentCode > 0 ? row.Cell(cParentCode).GetString().Trim() : "";
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            long? parentId = null;
            if (!string.IsNullOrWhiteSpace(parentCode))
            {
                parentId = await db.ProductCategories
                    .IgnoreQueryFilters()
                    .Where(x => x.TenantId == tenantId && x.OrgId == orgId && x.Code == parentCode)
                    .Select(x => (long?)x.Id)
                    .FirstOrDefaultAsync(ct);
                if (parentId is null)
                {
                    messages.Add($"第 {row.RowNumber()} 行父级编码不存在：{parentCode}");
                    skipped++;
                    continue;
                }
            }

            var isActive = cActive > 0 ? ParseActiveStatus(row.Cell(cActive).GetString()) : true;

            var existing = await db.ProductCategories
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.OrgId == orgId && x.Code == code, ct);
            if (existing is null)
            {
                db.ProductCategories.Add(new Core.Entities.ProductCategory
                {
                    TenantId = tenantId,
                    OrgId = orgId,
                    Code = code,
                    Name = name,
                    ParentId = parentId,
                    IsActive = isActive
                });
                created++;
            }
            else
            {
                existing.Code = code;
                existing.Name = name;
                existing.ParentId = parentId;
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

        return new ProductCategoryImportResultDto(created, updated, skipped, messages);
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
            "类别编号" or "categorycode" or "code" => "code",
            "类别名称" or "categoryname" or "name" => "name",
            "父级编号" or "parentcode" or "parent" => "parentcode",
            "是否激活" or "状态" or "isactive" or "active" => "active",
            _ => t.ToLowerInvariant()
        };
    }

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
}
