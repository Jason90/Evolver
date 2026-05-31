using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/production/boms")]
[Authorize]
public sealed class BomsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BomHeaderDto>>> List(CancellationToken ct)
    {
        var rows = await db.BomHeaders
            .AsNoTracking()
            .Include(x => x.FinishedProduct)
            .Include(x => x.Lines).ThenInclude(l => l.ComponentProduct)
            .OrderByDescending(x => x.UpdateTime)
            .ToListAsync(ct);

        var userMap = await ResolveUserMapAsync(rows.Select(x => x.UpdateBy), ct);
        return Ok(rows.Select(x => ToDto(x, userMap)).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<BomHeaderDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.BomHeaders
            .AsNoTracking()
            .Include(x => x.FinishedProduct)
            .Include(x => x.Lines).ThenInclude(l => l.ComponentProduct)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null)
            return NotFound();

        var userMap = await ResolveUserMapAsync(new[] { row.UpdateBy }, ct);
        return Ok(ToDto(row, userMap));
    }

    [HttpPost]
    public async Task<ActionResult<BomHeaderDto>> Create([FromBody] UpsertBomHeaderDto dto, CancellationToken ct)
    {
        var finishedProduct = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == dto.FinishedProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (finishedProduct is null)
            return BadRequest("成品不存在。");

        var duplicated = await db.BomHeaders.AnyAsync(x => x.FinishedProductId == dto.FinishedProductId && x.Version == dto.Version, ct);
        if (duplicated)
            return Conflict("该成品的版本号已存在。");

        var header = new BomHeader
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            FinishedProductId = dto.FinishedProductId,
            Version = dto.Version,
            IsActive = dto.IsActive,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo
        };

        var lineSort = 1;
        foreach (var l in dto.Lines.OrderBy(x => x.SortOrder))
        {
            var component = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(x => x.Id == l.ComponentProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
            if (component is null)
                return BadRequest($"组件不存在：{l.ComponentProductId}");

            header.Lines.Add(new BomLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ComponentProductId = l.ComponentProductId,
                Quantity = l.Quantity,
                Unit = string.IsNullOrWhiteSpace(l.Unit) ? "pcs" : l.Unit.Trim(),
                SortOrder = l.SortOrder <= 0 ? lineSort : l.SortOrder,
                ScrapRate = l.ScrapRate
            });
            lineSort++;
        }

        db.BomHeaders.Add(header);
        await db.SaveChangesAsync(ct);

        var row = await db.BomHeaders
            .AsNoTracking()
            .Include(x => x.FinishedProduct)
            .Include(x => x.Lines).ThenInclude(l => l.ComponentProduct)
            .FirstAsync(x => x.Id == header.Id, ct);
        var userMap = await ResolveUserMapAsync(new[] { row.UpdateBy }, ct);
        return Ok(ToDto(row, userMap));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertBomHeaderDto dto, CancellationToken ct)
    {
        var header = await db.BomHeaders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (header is null)
            return NotFound();

        var duplicated = await db.BomHeaders
            .AnyAsync(x => x.Id != id && x.FinishedProductId == dto.FinishedProductId && x.Version == dto.Version, ct);
        if (duplicated)
            return Conflict("该成品的版本号已存在。");

        header.FinishedProductId = dto.FinishedProductId;
        header.Version = dto.Version;
        header.IsActive = dto.IsActive;
        header.EffectiveFrom = dto.EffectiveFrom;
        header.EffectiveTo = dto.EffectiveTo;

        db.BomLines.RemoveRange(header.Lines);
        header.Lines.Clear();
        var lineSort = 1;
        foreach (var l in dto.Lines.OrderBy(x => x.SortOrder))
        {
            header.Lines.Add(new BomLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ComponentProductId = l.ComponentProductId,
                Quantity = l.Quantity,
                Unit = string.IsNullOrWhiteSpace(l.Unit) ? "pcs" : l.Unit.Trim(),
                SortOrder = l.SortOrder <= 0 ? lineSort : l.SortOrder,
                ScrapRate = l.ScrapRate
            });
            lineSort++;
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var header = await db.BomHeaders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (header is null)
            return NotFound();
        db.BomHeaders.Remove(header);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static BomHeaderDto ToDto(BomHeader x, Dictionary<long, string?> userMap)
        => new(
            x.Id,
            x.FinishedProductId,
            x.FinishedProduct.Code,
            x.FinishedProduct.Name,
            x.Version,
            x.IsActive,
            x.EffectiveFrom,
            x.EffectiveTo,
            x.Lines.OrderBy(l => l.SortOrder).Select(l => new BomLineDto(
                l.Id, l.ComponentProductId, l.ComponentProduct.Code, l.ComponentProduct.Name, l.Quantity, l.Unit, l.SortOrder, l.ScrapRate
            )).ToList(),
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value)
        );

    private async Task<Dictionary<long, string?>> ResolveUserMapAsync(IEnumerable<int?> updateBys, CancellationToken ct)
    {
        var ids = updateBys.Where(x => x is not null).Select(x => (long)x!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, string?>();
        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);
    }
}
