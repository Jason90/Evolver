using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class UnitSpreadsheetService(AppDbContext db, ITenantContext tenant)
{
    private const string SheetName = "Units";

    public async Task<UnitImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new UnitImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerMap = MapHeaders(range.FirstRow());
        if (!headerMap.TryGetValue("code", out var cCode) || !headerMap.TryGetValue("name", out var cName))
            return new UnitImportResultDto(0, 0, 0, new[] { "未找到单位编号或单位名称列。" });

        headerMap.TryGetValue("active", out var cActive);

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

            var isActive = cActive > 0 ? ParseActiveStatus(row.Cell(cActive).GetString()) : true;

            var existing = await db.Units
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.Code == code, ct);

            if (existing is null)
            {
                db.Units.Add(new Unit
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Code = code,
                    Name = name,
                    IsActive = isActive
                });
                created++;
            }
            else
            {
                existing.Name = name;
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

        return new UnitImportResultDto(created, updated, skipped, messages);
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
            "单位编号" or "code" or "unitcode" => "code",
            "单位名称" or "name" or "unitname" => "name",
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
