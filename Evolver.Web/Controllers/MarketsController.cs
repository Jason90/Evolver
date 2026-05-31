using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Evolver.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/markets")]
[Authorize]
public sealed class MarketsController(AppDbContext db, ITenantContext tenant, MarketSpreadsheetService spreadsheet) : ControllerBase
{
    [HttpGet]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Query)]
    public async Task<ActionResult<IReadOnlyList<MarketListItemDto>>> List(CancellationToken ct)
    {
        var rows = await db.Markets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.RentAmount,
                x.Address,
                x.Phone,
                x.Website,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy
            })
            .ToListAsync(ct);

        var userIds = rows
            .Select(x => x.UpdateBy)
            .Where(x => x is not null)
            .Select(x => (long)x!.Value)
            .Distinct()
            .ToList();

        var userMap = await db.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);

        return Ok(rows.Select(x => new MarketListItemDto(
            x.Id,
            x.Name,
            x.RentAmount,
            x.Address,
            x.Phone,
            x.Website,
            x.Remark,
            x.IsActive,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value))).ToList());
    }

    [HttpGet("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Query)]
    public async Task<ActionResult<MarketListItemDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Markets
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (row is null)
            return NotFound();

        var userName = row.UpdateBy is null
            ? null
            : await db.Users.AsNoTracking()
                .Where(u => u.Id == row.UpdateBy.Value)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct);

        return Ok(new MarketListItemDto(
            row.Id,
            row.Name,
            row.RentAmount,
            row.Address,
            row.Phone,
            row.Website,
            row.Remark,
            row.IsActive,
            row.UpdateTime,
            row.UpdateBy,
            userName));
    }

    [HttpPost]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Create)]
    public async Task<ActionResult<MarketListItemDto>> Create([FromBody] UpsertMarketDto dto, CancellationToken ct)
    {
        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("市场名称不能为空。");

        var exists = await db.Markets
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.TenantId == tenant.TenantId
                     && x.OrgId == tenant.OrgId
                     && x.Name.ToUpper() == name.ToUpper(),
                ct);
        if (exists)
            return Conflict("市场名称已存在。");

        var entity = new Market
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Name = name,
            RentAmount = dto.RentAmount < 0 ? 0 : dto.RentAmount,
            Address = Normalize(dto.Address),
            Phone = Normalize(dto.Phone),
            Website = Normalize(dto.Website),
            Remark = Normalize(dto.Remark),
            IsActive = dto.IsActive
        };
        db.Markets.Add(entity);
        await db.SaveChangesAsync(ct);
        return Ok(new MarketListItemDto(
            entity.Id,
            entity.Name,
            entity.RentAmount,
            entity.Address,
            entity.Phone,
            entity.Website,
            entity.Remark,
            entity.IsActive,
            entity.UpdateTime,
            entity.UpdateBy,
            null));
    }

    [HttpPut("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Update)]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertMarketDto dto, CancellationToken ct)
    {
        var entity = await db.Markets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        var name = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("市场名称不能为空。");

        var used = await db.Markets
            .IgnoreQueryFilters()
            .AnyAsync(
                x => x.Id != id
                     && x.TenantId == tenant.TenantId
                     && x.OrgId == tenant.OrgId
                     && x.Name.ToUpper() == name.ToUpper(),
                ct);
        if (used)
            return Conflict("市场名称已存在。");

        entity.Name = name;
        entity.RentAmount = dto.RentAmount < 0 ? 0 : dto.RentAmount;
        entity.Address = Normalize(dto.Address);
        entity.Phone = Normalize(dto.Phone);
        entity.Website = Normalize(dto.Website);
        entity.Remark = Normalize(dto.Remark);
        entity.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Delete)]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Markets
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (entity is null)
            return NotFound();

        db.Markets.Remove(entity);
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
            return Ok("市场存在关联数据，已自动改为停用。");
        }
    }

    [HttpPost("import")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Import)]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<MarketImportResultDto>> Import(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("请选择要导入的 Excel 文件。");
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest("仅支持 .xlsx 格式。");

        await using var stream = file.OpenReadStream();
        var result = await spreadsheet.ImportAsync(stream, ct);
        return Ok(result);
    }

    [HttpGet("active")]
    [RequirePermission(NavSystemSettingsPermissionCodes.Markets.Query)]
    public async Task<ActionResult<IReadOnlyList<MarketListItemDto>>> ListActive(CancellationToken ct)
    {
        var rows = await db.Markets.AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new MarketListItemDto(
                x.Id,
                x.Name,
                x.RentAmount,
                x.Address,
                x.Phone,
                x.Website,
                x.Remark,
                x.IsActive,
                x.UpdateTime,
                x.UpdateBy,
                null))
            .ToListAsync(ct);

        return Ok(rows);
    }

    private static string? Normalize(string? value)
    {
        var t = value?.Trim();
        return string.IsNullOrWhiteSpace(t) ? null : t;
    }
}
