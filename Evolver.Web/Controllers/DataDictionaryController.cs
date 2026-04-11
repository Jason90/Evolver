using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Evolver.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/data-dictionary")]
[Authorize]
public sealed class DataDictionaryController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet("items")]
    [RequirePermission("dictionary.read")]
    public async Task<ActionResult<IReadOnlyList<DataDictionaryItemDto>>> ListItems([FromQuery] string? category, CancellationToken ct)
    {
        var q = db.DataDictionaryItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(x => x.CategoryCode == category.Trim());

        var rows = await q
            .OrderBy(x => x.CategoryCode)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.ItemCode)
            .Select(x => new DataDictionaryItemDto(
                x.Id,
                x.CategoryCode,
                x.ItemCode,
                x.DisplayName,
                x.ItemValue,
                x.SortOrder,
                x.IsActive))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("items/{id:long}")]
    [RequirePermission("dictionary.read")]
    public async Task<ActionResult<DataDictionaryItemDto>> GetItem(long id, CancellationToken ct)
    {
        var x = await db.DataDictionaryItems.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        if (x is null)
            return NotFound();

        return Ok(new DataDictionaryItemDto(
            x.Id,
            x.CategoryCode,
            x.ItemCode,
            x.DisplayName,
            x.ItemValue,
            x.SortOrder,
            x.IsActive));
    }

    [HttpPost("items")]
    [RequirePermission("dictionary.write")]
    public async Task<ActionResult<DataDictionaryItemDto>> UpsertItem([FromBody] UpsertDataDictionaryItemDto dto, CancellationToken ct)
    {
        var cat = dto.CategoryCode.Trim();
        var code = dto.ItemCode.Trim();
        var name = dto.DisplayName.Trim();
        if (string.IsNullOrEmpty(cat) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return BadRequest("CategoryCode, ItemCode, DisplayName are required.");

        var existing = await db.DataDictionaryItems.FirstOrDefaultAsync(
            x => x.CategoryCode == cat && x.ItemCode == code, ct);

        if (existing is null)
        {
            existing = new DataDictionaryItem
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                CategoryCode = cat,
                ItemCode = code,
                DisplayName = name,
                ItemValue = string.IsNullOrWhiteSpace(dto.ItemValue) ? null : dto.ItemValue.Trim(),
                SortOrder = dto.SortOrder,
                IsActive = dto.IsActive,
                IsDeleted = false
            };
            db.DataDictionaryItems.Add(existing);
        }
        else
        {
            existing.DisplayName = name;
            existing.ItemValue = string.IsNullOrWhiteSpace(dto.ItemValue) ? null : dto.ItemValue.Trim();
            existing.SortOrder = dto.SortOrder;
            existing.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new DataDictionaryItemDto(
            existing.Id,
            existing.CategoryCode,
            existing.ItemCode,
            existing.DisplayName,
            existing.ItemValue,
            existing.SortOrder,
            existing.IsActive));
    }

    [HttpDelete("items/{id:long}")]
    [RequirePermission("dictionary.write")]
    public async Task<ActionResult> DeleteItem(long id, CancellationToken ct)
    {
        var row = await db.DataDictionaryItems.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return NotFound();

        row.IsDeleted = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
