using System.Globalization;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Services;

public sealed class CustomerSpreadsheetService(AppDbContext db, ITenantContext tenant)
{
    private const string SheetName = "Customers";

    public async Task<CustomerImportResultDto> ImportAsync(Stream xlsxStream, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);
        if (!wb.TryGetWorksheet(SheetName, out var ws))
            ws = wb.Worksheets.First();

        var range = ws.RangeUsed();
        if (range is null)
            return new CustomerImportResultDto(0, 0, 0, new[] { "工作表为空。" });

        var headerMap = MapHeaders(range.FirstRow());
        if (!headerMap.TryGetValue("name", out var cName))
            return new CustomerImportResultDto(0, 0, 0, new[] { "未找到客户姓名列。" });

        headerMap.TryGetValue("category", out var cCategory);
        headerMap.TryGetValue("gender", out var cGender);
        headerMap.TryGetValue("birthday", out var cBirthday);
        headerMap.TryGetValue("jobtitle", out var cJobTitle);
        headerMap.TryGetValue("phone", out var cPhone);
        headerMap.TryGetValue("email", out var cEmail);
        headerMap.TryGetValue("remark", out var cRemark);
        headerMap.TryGetValue("active", out var cActive);

        var categoryRows = await db.CustomerCategories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .Select(x => new { x.Id, x.CategoryCode, x.Name })
            .ToListAsync(ct);
        var categoryByCode = categoryRows
            .Where(x => !string.IsNullOrWhiteSpace(x.CategoryCode))
            .ToDictionary(x => x.CategoryCode.Trim().ToUpperInvariant(), x => x.Id);
        var categoryByName = categoryRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name.Trim().ToUpperInvariant(), x => x.Id);

        var existingRows = await db.Customers
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .ToListAsync(ct);
        var existingByPhone = existingRows
            .Where(x => !string.IsNullOrWhiteSpace(x.Phone))
            .GroupBy(x => x.Phone!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

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

            var phone = cPhone > 0 ? NullIfEmpty(row.Cell(cPhone).GetString()) : null;
            var email = cEmail > 0 ? NullIfEmpty(row.Cell(cEmail).GetString()) : null;
            var gender = cGender > 0 ? NormalizeGender(row.Cell(cGender).GetString()) : null;
            var birthday = cBirthday > 0 ? ParseBirthday(row.Cell(cBirthday).GetString()) : null;
            var jobTitle = cJobTitle > 0 ? NullIfEmpty(row.Cell(cJobTitle).GetString()) : null;
            var remark = cRemark > 0 ? NullIfEmpty(row.Cell(cRemark).GetString()) : null;
            var isActive = cActive > 0 ? ParseActiveStatus(row.Cell(cActive).GetString()) : true;

            long? categoryRefId = null;
            if (cCategory > 0)
            {
                var rawCategory = row.Cell(cCategory).GetString().Trim();
                if (!string.IsNullOrWhiteSpace(rawCategory))
                {
                    var key = rawCategory.ToUpperInvariant();
                    if (categoryByCode.TryGetValue(key, out var categoryId)
                        || categoryByName.TryGetValue(key, out categoryId))
                    {
                        categoryRefId = categoryId;
                    }
                    else
                    {
                        messages.Add($"客户“{name}”的客户类别“{rawCategory}”不存在，已跳过该类别。");
                    }
                }
            }

            Customer? existing = null;
            if (!string.IsNullOrWhiteSpace(phone) && existingByPhone.TryGetValue(phone, out var byPhone))
                existing = byPhone;
            else
                existing = existingRows.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                var entity = new Customer
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    Code = $"CUS-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    Name = name,
                    CustomerType = CustomerType.Member,
                    CustomerCategoryRefId = categoryRefId,
                    Gender = gender,
                    Birthday = birthday,
                    JobTitle = jobTitle,
                    Phone = phone,
                    Email = email,
                    Remark = remark,
                    IsActive = isActive
                };
                db.Customers.Add(entity);
                existingRows.Add(entity);
                if (!string.IsNullOrWhiteSpace(phone))
                    existingByPhone[phone] = entity;
                created++;
            }
            else
            {
                existing.Name = name;
                existing.CustomerCategoryRefId = categoryRefId;
                existing.Gender = gender;
                existing.Birthday = birthday;
                existing.JobTitle = jobTitle;
                existing.Phone = phone;
                existing.Email = email;
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

        return new CustomerImportResultDto(created, updated, skipped, messages);
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
            "客户类别" or "category" or "customercategory" => "category",
            "姓名" or "客户姓名" or "name" => "name",
            "性别" or "gender" => "gender",
            "生日" or "birth" or "birthday" => "birthday",
            "职务" or "jobtitle" or "title" => "jobtitle",
            "手机" or "电话" or "手机号" or "phone" => "phone",
            "电子邮箱" or "邮箱" or "email" => "email",
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

    private static DateOnly? ParseBirthday(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;

        var text = s.Trim();
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return DateOnly.FromDateTime(dt);
        return null;
    }

    private static string? NormalizeGender(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        return s.Trim().ToUpperInvariant() switch
        {
            "M" or "男" or "MALE" => "男",
            "F" or "女" or "FEMALE" => "女",
            "未知" or "UNKNOWN" or "U" => "未知",
            _ => s.Trim()
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
