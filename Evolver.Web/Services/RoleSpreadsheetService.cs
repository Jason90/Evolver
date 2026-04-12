using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class RoleSpreadsheetService(
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    ITenantContext tenant)
{
    private const string SheetName = "Roles";

    public async Task<Stream> ExportAsync(CancellationToken ct)
    {
        var roles = await roleManager.Roles.AsNoTracking()
            .Where(r => r.TenantId == tenant.TenantId && !r.IsDeleted)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        var orgIds = roles.Select(r => (long)r.OrgId).Distinct().ToList();
        var orgNames = await db.Organizations.AsNoTracking()
            .Where(o => orgIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Name })
            .ToListAsync(ct);
        var orgMap = orgNames.ToDictionary(x => x.Id, x => x.Name);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName);
        var headers = new[] { "Id", "Name", "NormalizedName", "TenantId", "OrgId", "OrgName", "UpdateBy", "UpdateTime" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var row = 2;
        foreach (var r in roles)
        {
            var oid = (long)r.OrgId;
            ws.Cell(row, 1).Value = r.Id;
            ws.Cell(row, 2).Value = r.Name ?? "";
            ws.Cell(row, 3).Value = r.NormalizedName ?? "";
            ws.Cell(row, 4).Value = r.TenantId;
            ws.Cell(row, 5).Value = r.OrgId;
            ws.Cell(row, 6).Value = orgMap.GetValueOrDefault(oid) ?? "";
            if (r.UpdateBy is { } ub)
                ws.Cell(row, 7).Value = ub;
            ws.Cell(row, 8).Value = r.UpdateTime?.ToString("o", CultureInfo.InvariantCulture) ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<RoleImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new RoleImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerRow = range.FirstRow();
        var colMap = MapHeaders(headerRow);
        if (!colMap.TryGetValue("name", out var cName) || cName < 1)
            return new RoleImportResultDto(0, 0, 0, new[] { "未找到 Name 列。" });

        colMap.TryGetValue("orgid", out var cOrgId);

        var messages = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var name = row.Cell(cName).GetString().Trim();
            if (string.IsNullOrEmpty(name))
            {
                skipped++;
                continue;
            }

            int? orgId = null;
            if (cOrgId > 0)
            {
                var raw = row.Cell(cOrgId).GetString().Trim();
                if (!string.IsNullOrEmpty(raw) && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    orgId = parsed;
            }

            if (orgId is { } oid)
            {
                var okOrg = await db.Organizations.AsNoTracking()
                    .AnyAsync(o => o.Id == oid && o.TenantId == tenant.TenantId, ct);
                if (!okOrg)
                {
                    messages.Add($"第 {row.RowNumber()} 行：组织 Id {oid} 不存在或不属于当前租户。");
                    skipped++;
                    continue;
                }
            }

            var normalized = roleManager.NormalizeKey(name);
            var existing = await roleManager.Roles.FirstOrDefaultAsync(
                r => r.TenantId == tenant.TenantId && !r.IsDeleted && r.NormalizedName == normalized,
                ct);

            if (existing is null)
            {
                var appRole = new AppRole
                {
                    Name = name,
                    TenantId = tenant.TenantId,
                    OrgId = orgId ?? tenant.OrgId
                };
                var res = await roleManager.CreateAsync(appRole);
                if (!res.Succeeded)
                {
                    messages.Add($"第 {row.RowNumber()} 行：{string.Join("；", res.Errors.Select(e => e.Description))}");
                    skipped++;
                    continue;
                }

                created++;
                continue;
            }

            existing.Name = name;
            if (orgId is { } o)
                existing.OrgId = o;

            var upd = await roleManager.UpdateAsync(existing);
            if (!upd.Succeeded)
            {
                messages.Add($"第 {row.RowNumber()} 行更新失败：{string.Join("；", upd.Errors.Select(e => e.Description))}");
                skipped++;
                continue;
            }

            updated++;
        }

        return new RoleImportResultDto(created, updated, skipped, messages);
    }

    private static Dictionary<string, int> MapHeaders(IXLRangeRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = cell.GetString().Trim().Replace(" ", "").ToLowerInvariant();
            if (string.IsNullOrEmpty(key))
                continue;
            map[key] = cell.Address.ColumnNumber;
        }

        return map;
    }
}
