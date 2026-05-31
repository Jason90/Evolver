using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class SupplierSpreadsheetService(AppDbContext db, ITenantContext tenant)
{
    private const string SheetName = "Suppliers";

    public async Task<SupplierImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new SupplierImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerMap = MapHeaders(range.FirstRow());
        if (!headerMap.TryGetValue("name", out var cName))
            return new SupplierImportResultDto(0, 0, 0, new[] { "未找到供应商名称列。" });

        headerMap.TryGetValue("address", out var cAddress);
        headerMap.TryGetValue("phone", out var cPhone);
        headerMap.TryGetValue("website", out var cWebsite);
        headerMap.TryGetValue("remark", out var cRemark);
        headerMap.TryGetValue("active", out var cActive);

        var created = 0;
        var updated = 0;
        var skipped = 0;
        var messages = new List<string>();

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var name = row.Cell(cName).GetString().Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                skipped++;
                continue;
            }

            var address = cAddress > 0 ? NullIfEmpty(row.Cell(cAddress).GetString()) : null;
            var phone = cPhone > 0 ? NullIfEmpty(row.Cell(cPhone).GetString()) : null;
            var website = cWebsite > 0 ? NullIfEmpty(row.Cell(cWebsite).GetString()) : null;
            var remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString()) : null;
            var isActive = cActive > 0 ? ParseActiveStatus(row.Cell(cActive).GetString()) : true;

            var normalizedName = name.ToUpperInvariant();
            var existing = await db.Suppliers
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenant.TenantId
                         && x.OrgId == tenant.OrgId
                         && x.Name.ToUpper() == normalizedName,
                    ct);

            if (existing is null)
            {
                db.Suppliers.Add(new Supplier
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Code = $"SUP-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
                    Name = name,
                    Address = address,
                    Phone = phone,
                    Website = website,
                    Remark = remark,
                    IsActive = isActive
                });
                created++;
            }
            else
            {
                existing.Name = name;
                existing.Address = address;
                existing.Phone = phone;
                existing.Website = website;
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

        return new SupplierImportResultDto(created, updated, skipped, messages);
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
            "供应商名称" or "name" or "suppliername" => "name",
            "地址" or "address" => "address",
            "电话" or "联系电话" or "phone" => "phone",
            "网址" or "website" or "url" => "website",
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
