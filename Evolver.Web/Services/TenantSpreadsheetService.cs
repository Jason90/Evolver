using ClosedXML.Excel;
using Evolver.Shared.Dtos;

namespace Evolver.Web.Services;

/// <summary>平台租户批量开通：从 Excel 导入多行 <see cref="ProvisionTenantRequestDto"/>。</summary>
public sealed class TenantSpreadsheetService(TenantProvisioningService provisioning)
{
    private const string SheetName = "Tenants";

    public async Task<TenantImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new TenantImportResultDto(0, 0, new[] { "工作表为空。" });

        var headerRow = range.FirstRow();
        var colMap = MapHeaders(headerRow);
        if (!colMap.TryGetValue("tenantname", out var cTenant) || cTenant < 1)
            return new TenantImportResultDto(0, 0, new[] { "未找到 TenantName（租户名称）列。" });
        if (!colMap.TryGetValue("adminusername", out var cUser) || cUser < 1)
            return new TenantImportResultDto(0, 0, new[] { "未找到 AdminUserName（管理员用户名）列。" });
        if (!colMap.TryGetValue("adminpassword", out var cPwd) || cPwd < 1)
            return new TenantImportResultDto(0, 0, new[] { "未找到 AdminPassword（管理员密码）列。" });

        colMap.TryGetValue("rootorgname", out var cRoot);
        colMap.TryGetValue("adminemail", out var cEmail);
        colMap.TryGetValue("isactive", out var cIsActive);
        colMap.TryGetValue("expireat", out var cExpireAt);
        colMap.TryGetValue("remark", out var cRemark);

        var messages = new List<string>();
        var created = 0;
        var skipped = 0;

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var tenantName = row.Cell(cTenant).GetString().Trim();
            var adminUser = row.Cell(cUser).GetString().Trim();
            var adminPwd = row.Cell(cPwd).GetString();
            if (string.IsNullOrEmpty(tenantName) && string.IsNullOrEmpty(adminUser))
            {
                skipped++;
                continue;
            }

            if (string.IsNullOrEmpty(tenantName) || string.IsNullOrEmpty(adminUser))
            {
                messages.Add($"第 {row.RowNumber()} 行：TenantName 与 AdminUserName 须同时填写。");
                skipped++;
                continue;
            }

            var rootOrg = cRoot > 0 ? NullIfEmpty(row.Cell(cRoot).GetString().Trim()) : null;
            var adminEmail = cEmail > 0 ? NullIfEmpty(row.Cell(cEmail).GetString().Trim()) : null;
            var remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString().Trim()) : null;
            var isActive = true;
            if (cIsActive > 0)
            {
                var rawStatus = row.Cell(cIsActive).GetString().Trim();
                if (!string.IsNullOrEmpty(rawStatus))
                    isActive = !string.Equals(rawStatus, "停用", StringComparison.OrdinalIgnoreCase) && !string.Equals(rawStatus, "false", StringComparison.OrdinalIgnoreCase) && rawStatus != "0";
            }
            DateTime? expireAt = null;
            if (cExpireAt > 0)
            {
                var raw = row.Cell(cExpireAt).GetString().Trim();
                if (!string.IsNullOrEmpty(raw) && DateTime.TryParse(raw, out var parsed))
                    expireAt = parsed.Date;
            }

            if (string.IsNullOrEmpty(adminPwd))
            {
                messages.Add($"第 {row.RowNumber()} 行：AdminPassword 不能为空。");
                skipped++;
                continue;
            }

            try
            {
                var dto = new ProvisionTenantRequestDto(tenantName, adminUser, adminPwd, rootOrg, adminEmail, isActive, expireAt, remark);
                await provisioning.ProvisionAsync(dto, ct);
                created++;
            }
            catch (Exception ex)
            {
                messages.Add($"第 {row.RowNumber()} 行「{tenantName}」：{ex.Message}");
                skipped++;
            }
        }

        return new TenantImportResultDto(created, skipped, messages);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static Dictionary<string, int> MapHeaders(IXLRangeRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var key = NormalizeHeader(cell.GetString());
            if (key.Length == 0)
                continue;
            map[key] = cell.Address.ColumnNumber;
        }

        return map;
    }

    private static string NormalizeHeader(string raw)
    {
        var t = raw.Trim();
        return t.ToLowerInvariant() switch
        {
            "tenantname" or "租户名称" or "公司名称" or "新公司" or "租户名" => "tenantname",
            "rootorgname" or "根组织" or "根组织名称" => "rootorgname",
            "adminusername" or "管理员用户名" or "管理员登录名" => "adminusername",
            "adminpassword" or "管理员密码" or "密码" or "初始密码" => "adminpassword",
            "adminemail" or "邮箱" or "管理员邮箱" or "email" => "adminemail",
            "isactive" or "状态" or "是否激活" => "isactive",
            "expireat" or "到期时间" or "到期日期" => "expireat",
            "remark" or "备注" => "remark",
            _ => t.ToLowerInvariant()
        };
    }
}
