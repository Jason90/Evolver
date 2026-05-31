using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Evolver.Web.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public sealed class CustomersController(AppDbContext db, ITenantContext tenant, CustomerSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Query)]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.CustomerCategoryRefId,
                CategoryCode = x.CustomerCategory == null ? null : x.CustomerCategory.CategoryCode,
                CategoryName = x.CustomerCategory == null ? null : x.CustomerCategory.Name,
                x.Name,
                x.Gender,
                x.Birthday,
                x.JobTitle,
                x.Phone,
                x.Email,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .ToListAsync(ct);

        var userIds = rows.Where(x => x.UpdateBy is not null).Select(x => (long)x.UpdateBy!.Value).Distinct().ToList();
        var userMap = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        return Ok(rows.Select(x => new CustomerListItemDto(
            x.Id,
            x.CustomerCategoryRefId,
            x.CategoryCode,
            x.CategoryName,
            x.Name,
            x.Gender,
            x.Birthday,
            x.JobTitle,
            x.Phone,
            x.Email,
            x.Remark,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Query)]
    public async Task<ActionResult<CustomerListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .Select(x => new
            {
                x.Id,
                x.CustomerCategoryRefId,
                CategoryCode = x.CustomerCategory == null ? null : x.CustomerCategory.CategoryCode,
                CategoryName = x.CustomerCategory == null ? null : x.CustomerCategory.Name,
                x.Name,
                x.Gender,
                x.Birthday,
                x.JobTitle,
                x.Phone,
                x.Email,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .FirstOrDefaultAsync(ct);
        if (row is null) return NotFound();

        var userName = row.UpdateBy is null
            ? null
            : await db.Users.AsNoTracking()
                .Where(u => u.Id == row.UpdateBy.Value)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct);

        return Ok(new CustomerListItemDto(
            row.Id,
            row.CustomerCategoryRefId,
            row.CategoryCode,
            row.CategoryName,
            row.Name,
            row.Gender,
            row.Birthday,
            row.JobTitle,
            row.Phone,
            row.Email,
            row.Remark,
            row.IsActive,
            row.UpdateTime,
            row.UpdateBy,
            userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Create)]
    public async Task<ActionResult<CustomerListItemDto>> Create([FromBody] UpsertCustomerDto dto, CancellationToken ct)
    {
        var validate = await ValidateInputAsync(dto, null, ct);
        if (!validate.Ok)
            return BadRequest(validate.Error);

        var entity = new Customer
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Code = $"CUS-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Name = validate.Name!,
            CustomerType = CustomerType.Member,
            CustomerCategoryRefId = validate.CustomerCategoryRefId,
            Gender = validate.Gender,
            Birthday = validate.Birthday,
            JobTitle = validate.JobTitle,
            Phone = validate.Phone,
            Email = validate.Email,
            Remark = validate.Remark,
            IsActive = dto.IsActive
        };

        db.Customers.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(new CustomerListItemDto(
            entity.Id,
            entity.CustomerCategoryRefId,
            null,
            null,
            entity.Name,
            entity.Gender,
            entity.Birthday,
            entity.JobTitle,
            entity.Phone,
            entity.Email,
            entity.Remark,
            entity.IsActive,
            entity.UpdateTime,
            entity.UpdateBy,
            null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertCustomerDto dto, CancellationToken ct)
    {
        var entity = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null) return NotFound();

        var validate = await ValidateInputAsync(dto, id, ct);
        if (!validate.Ok)
            return BadRequest(validate.Error);

        entity.Name = validate.Name!;
        entity.CustomerCategoryRefId = validate.CustomerCategoryRefId;
        entity.Gender = validate.Gender;
        entity.Birthday = validate.Birthday;
        entity.JobTitle = validate.JobTitle;
        entity.Phone = validate.Phone;
        entity.Email = validate.Email;
        entity.Remark = validate.Remark;
        entity.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Customers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null) return NotFound();

        db.Customers.Remove(entity);
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
            return Ok("客户存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<CustomerImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("import-template")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Query)]
    public ActionResult DownloadImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Customers");

        var headers = new[] { "客户类别", "姓名", "性别", "生日", "职务", "手机", "电子邮箱", "备注", "是否激活" };
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        sheet.Cell(2, 1).Value = "示例类别";
        sheet.Cell(2, 2).Value = "张三";
        sheet.Cell(2, 3).Value = "男";
        sheet.Cell(2, 4).Value = "1990-01-01";
        sheet.Cell(2, 5).Value = "经理";
        sheet.Cell(2, 6).Value = "13800000000";
        sheet.Cell(2, 7).Value = "demo@example.com";
        sheet.Cell(2, 8).Value = "示例备注";
        sheet.Cell(2, 9).Value = "是";

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "customers_import_template.xlsx");
    }

    [HttpGet("active")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Customers.Query)]
    public async Task<ActionResult<IReadOnlyList<CustomerListItemDto>>> ListActive(CancellationToken ct)
    {
        var rows = await db.Customers
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Name)
            .Select(x => new CustomerListItemDto(
                x.Id,
                x.CustomerCategoryRefId,
                x.CustomerCategory == null ? null : x.CustomerCategory.CategoryCode,
                x.CustomerCategory == null ? null : x.CustomerCategory.Name,
                x.Name,
                x.Gender,
                x.Birthday,
                x.JobTitle,
                x.Phone,
                x.Email,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy,
                null))
            .ToListAsync(ct);

        return Ok(rows);
    }

    private async Task<(bool Ok, string? Error, string? Name, long? CustomerCategoryRefId, string? Gender, DateOnly? Birthday, string? JobTitle, string? Phone, string? Email, string? Remark)>
        ValidateInputAsync(UpsertCustomerDto dto, long? selfId, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "姓名不能为空。", null, null, null, null, null, null, null, null);

        var phone = Normalize(dto.Phone);
        var email = Normalize(dto.Email);
        if (!string.IsNullOrWhiteSpace(email) && !email.Contains('@'))
            return (false, "电子邮箱格式不正确。", null, null, null, null, null, null, null, null);

        long? categoryRefId = null;
        if (dto.CustomerCategoryRefId is { } cid)
        {
            var categoryExists = await db.CustomerCategories
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id == cid && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
            if (!categoryExists)
                return (false, "客户类别不存在。", null, null, null, null, null, null, null, null);
            categoryRefId = cid;
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var duplicatePhone = await db.Customers
                .IgnoreQueryFilters()
                .AnyAsync(x => x.Id != (selfId ?? 0)
                               && x.TenantId == tenant.TenantId
                               && x.OrgId == tenant.OrgId
                               && x.Phone == phone, ct);
            if (duplicatePhone)
                return (false, "手机号已存在。", null, null, null, null, null, null, null, null);
        }

        var gender = NormalizeGender(dto.Gender);
        var jobTitle = Normalize(dto.JobTitle);
        var remark = Normalize(dto.Remark);
        return (true, null, name, categoryRefId, gender, dto.Birthday, jobTitle, phone, email, remark);
    }

    private static string? Normalize(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }

    private static string? NormalizeGender(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
            return null;
        return gender.Trim() switch
        {
            "M" or "male" or "Male" or "男" => "男",
            "F" or "female" or "Female" or "女" => "女",
            _ => gender.Trim()
        };
    }
}
