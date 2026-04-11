using Evolver.Core.Entities;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/sales")]
[Authorize]
public sealed class SalesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SalesEntryDto>>> Get([FromQuery] string? market, CancellationToken ct)
    {
        IQueryable<SalesEntry> q = db.SalesEntries
            .AsNoTracking()
            .Include(x => x.Market)
            .Include(x => x.Product);

        if (!string.IsNullOrWhiteSpace(market))
            q = q.Where(x => x.Market.Name == market);

        var rows = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Market.Name)
            .ThenBy(x => x.Product.Code)
            .Take(500)
            .ToListAsync(ct);

        return Ok(rows.Select(x => new SalesEntryDto(
            Id: x.Id,
            Date: x.Date,
            Market: x.Market.Name,
            ProductCode: x.Product.Code,
            ProductName: x.Product.Name,
            UnitsSold: x.UnitsSold,
            UnitPrice: x.UnitPrice,
            SalesValue: x.SalesValue,
            Notes: x.Notes
        )).ToList());
    }
}
