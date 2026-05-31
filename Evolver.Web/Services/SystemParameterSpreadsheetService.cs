using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class SystemParameterSpreadsheetService(AppDbContext db, ITenantContext tenant)
{
    private const string SheetName = "Parameters";

    public async Task<SystemParameterImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new SystemParameterImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerMap = MapHeaders(range.FirstRow());
        if (!headerMap.TryGetValue("name", out var cName) || !headerMap.TryGetValue("key", out var cKey))
            return new SystemParameterImportResultDto(0, 0, 0, new[] { "未找到参数名称或参数键名列。" });

        headerMap.TryGetValue("value", out var cValue);
        headerMap.TryGetValue("builtin", out var cBuiltIn);
        headerMap.TryGetValue("remark", out var cRemark);
        headerMap.TryGetValue("active", out var cActive);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var messages = new List<string>();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var name = row.Cell(cName).GetString().Trim();
            var key = row.Cell(cKey).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(key))
            {
                skipped++;
                continue;
            }

            var value = cValue > 0 ? row.Cell(cValue).GetString().Trim() : "";
            var builtIn = cBuiltIn > 0 && ParseBool(row.Cell(cBuiltIn).GetString());
            var remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString()) : null;
            var isActive = cActive <= 0 || ParseActiveStatus(row.Cell(cActive).GetString());

            var existing = await db.SystemParameters
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.ParamKey == key,
                    ct);

            if (existing is null)
            {
                db.SystemParameters.Add(new SystemParameter
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Name = name,
                    ParamKey = key,
                    ParamValue = value,
                    IsSystemBuiltIn = builtIn,
                    Remark = remark,
                    IsActive = isActive
                });
                created++;
            }
            else
            {
                existing.Name = name;
                existing.ParamValue = value;
                existing.IsSystemBuiltIn = builtIn;
                existing.Remark = remark;
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

        return new SystemParameterImportResultDto(created, updated, skipped, messages);
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
            "参数名称" or "name" => "name",
            "参数键名" or "参数键" or "key" or "paramkey" => "key",
            "参数值" or "value" or "paramvalue" => "value",
            "系统内置" or "builtin" or "issystembuiltin" => "builtin",
            "备注" or "remark" => "remark",
            "是否激活" or "状态" or "isactive" or "active" => "active",
            _ => t.ToLowerInvariant()
        };
    }

    private static string? NullIfEmpty(string? s)
    {
        var t = s?.Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }

    private static bool ParseBool(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return false;
        var t = s.Trim();
        if (bool.TryParse(t, out var b))
            return b;
        if (int.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
            return n != 0;
        return t is "是" or "Y" or "y" or "yes" or "YES" or "true";
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
