using System.Globalization;
using System.Linq;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class UserSpreadsheetService(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    AppDbContext db,
    ITenantContext tenant)
{
    private const string SheetName = "Users";

    // 无 Password 列或单元格为空时，新建用户使用（与种子 Admin 默认一致）
    private const string DefaultImportPassword = "admin123";

    public async Task<Stream> ExportAsync(string? status, CancellationToken ct)
    {
        var query = userManager.Users.AsNoTracking()
            .Where(u => u.TenantId == tenant.TenantId);

        query = status?.ToLowerInvariant() switch
        {
            "active" => query.Where(u => u.IsActive),
            "inactive" => query.Where(u => !u.IsActive),
            _ => query
        };

        var users = await query.OrderBy(u => u.UserName).ToListAsync(ct);

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(SheetName);
        var headers = new[] { "Id", "UserName", "Email", "Phone", "Roles", "状态", "Password" };
        for (var c = 0; c < headers.Length; c++)
            ws.Cell(1, c + 1).Value = headers[c];

        var row = 2;
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            ws.Cell(row, 1).Value = u.Id;
            ws.Cell(row, 2).Value = u.UserName ?? "";
            ws.Cell(row, 3).Value = u.Email ?? "";
            ws.Cell(row, 4).Value = u.PhoneNumber ?? "";
            ws.Cell(row, 5).Value = string.Join(", ", roles);
            ws.Cell(row, 6).Value = u.IsActive ? "正常" : "停用";
            ws.Cell(row, 7).Value = "";
            row++;
        }

        ws.Columns().AdjustToContents();
        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    public async Task<UserImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new UserImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerRow = range.FirstRow();
        var colMap = MapHeaders(headerRow);
        if (!colMap.TryGetValue("username", out var cUser) || cUser < 1)
            return new UserImportResultDto(0, 0, 0, new[] { "未找到 UserName 列。" });

        colMap.TryGetValue("email", out var cEmail);
        colMap.TryGetValue("phone", out var cPhone);
        colMap.TryGetValue("roles", out var cRoles);
        colMap.TryGetValue("status", out var cStatus);
        colMap.TryGetValue("password", out var cPassword);

        var messages = new List<string>();
        var created = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var row in range.RowsUsed().Skip(1))
        {
            var userName = row.Cell(cUser).GetString().Trim();
            if (string.IsNullOrEmpty(userName))
            {
                skipped++;
                continue;
            }

            var email = cEmail > 0 ? NullIfEmpty(row.Cell(cEmail).GetString().Trim()) : null;
            var phone = cPhone > 0 ? NullIfEmpty(row.Cell(cPhone).GetString().Trim()) : null;
            var rolesRaw = cRoles > 0 ? row.Cell(cRoles).GetString() : "";
            var statusRaw = cStatus > 0 ? row.Cell(cStatus).GetString() : null;
            var password = cPassword > 0 ? NullIfEmpty(row.Cell(cPassword).GetString()) : null;

            var isActive = ParseActiveStatus(statusRaw);
            var roleNames = ParseRoles(rolesRaw);

            var normalized = userManager.NormalizeName(userName);
            var existing = normalized is null
                ? null
                : await userManager.Users.FirstOrDefaultAsync(
                    u => u.TenantId == tenant.TenantId && u.NormalizedUserName == normalized,
                    ct);
            if (existing is not null && existing.TenantId != tenant.TenantId)
            {
                messages.Add($"第 {row.RowNumber()} 行：「{userName}」不属于当前租户。");
                skipped++;
                continue;
            }

            if (existing is null)
            {
                var createPassword = string.IsNullOrEmpty(password) ? DefaultImportPassword : password;

                var user = new AppUser
                {
                    UserName = userName,
                    Email = email,
                    PhoneNumber = phone,
                    EmailConfirmed = true,
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    IsActive = isActive
                };

                var res = await userManager.CreateAsync(user, createPassword);
                if (!res.Succeeded)
                {
                    messages.Add($"第 {row.RowNumber()} 行：创建「{userName}」失败：{string.Join("; ", res.Errors.Select(e => e.Description))}");
                    skipped++;
                    continue;
                }

                await SyncRolesAsync(user, roleNames, ct);
                created++;
            }
            else
            {
                if (email is not null)
                    existing.Email = email;
                if (phone is not null)
                    existing.PhoneNumber = phone;
                existing.IsActive = isActive;

                var up = await userManager.UpdateAsync(existing);
                if (!up.Succeeded)
                {
                    messages.Add($"第 {row.RowNumber()} 行：更新「{userName}」失败：{string.Join("; ", up.Errors.Select(e => e.Description))}");
                    skipped++;
                    continue;
                }

                await SyncRolesAsync(existing, roleNames, ct);
                updated++;
            }
        }

        return new UserImportResultDto(created, updated, skipped, messages);
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
            "id" => "id",
            "username" or "用户" or "用户名" or "登录名" => "username",
            "email" or "邮箱" => "email",
            "phone" or "电话" or "手机" => "phone",
            "roles" or "角色" => "roles",
            "状态" or "status" or "启用" => "status",
            "password" or "初始密码" or "密码" => "password",
            _ => t
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

    private static List<string> ParseRoles(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new List<string>();
        return raw.Split(new[] { ',', ';', '，', '；' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task SyncRolesAsync(AppUser user, IReadOnlyList<string> roleNames, CancellationToken ct)
    {
        if (roleNames.Count == 0)
            return;

        var links = await db.UserRoles.Where(ur => ur.UserId == user.Id).ToListAsync(ct);
        db.UserRoles.RemoveRange(links);

        foreach (var r in roleNames)
        {
            var role = await roleManager.Roles.FirstOrDefaultAsync(
                x => x.TenantId == tenant.TenantId && x.Name == r,
                ct);
            if (role is null)
                continue;
            db.UserRoles.Add(new IdentityUserRole<long> { UserId = user.Id, RoleId = role.Id });
        }

        await db.SaveChangesAsync(ct);
    }
}
