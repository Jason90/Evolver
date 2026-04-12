using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> Get(CancellationToken ct)
    {
        var rows = await db.Products
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new ProductDto(x.Id, x.Code, x.Name, x.UnitPrice, x.UnitCost))
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ProductDto(x.Id, x.Code, x.Name, x.UnitPrice, x.UnitCost))
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return NotFound();

        return Ok(row);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> Create([FromBody] ProductCreateDto dto, CancellationToken ct)
    {
        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return BadRequest("Code and name are required.");

        var entity = new Product
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            Code = code,
            Name = name,
            UnitPrice = dto.UnitPrice,
            UnitCost = dto.UnitCost
        };

        db.Products.Add(entity);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("A product with this code already exists.");
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.Id },
            new ProductDto(entity.Id, entity.Code, entity.Name, entity.UnitPrice, entity.UnitCost));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, [FromBody] ProductUpdateDto dto, CancellationToken ct)
    {
        var entity = await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound();

        var code = dto.Code.Trim();
        var name = dto.Name.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            return BadRequest("Code and name are required.");

        entity.Code = code;
        entity.Name = name;
        entity.UnitPrice = dto.UnitPrice;
        entity.UnitCost = dto.UnitCost;

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return Conflict("A product with this code already exists.");
        }

        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null)
            return NotFound();

        db.Products.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
