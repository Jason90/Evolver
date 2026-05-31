using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/production/orders")]
[Authorize]
public sealed class ProductionOrdersController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductionOrderDto>>> List(CancellationToken ct)
    {
        var rows = await db.ProductionOrders
            .AsNoTracking()
            .Include(x => x.OutputProduct)
            .Include(x => x.MaterialLines).ThenInclude(x => x.MaterialProduct)
            .Include(x => x.WasteRecords).ThenInclude(x => x.Product)
            .OrderByDescending(x => x.UpdateTime)
            .ToListAsync(ct);

        var userMap = await ResolveUserMapAsync(rows.Select(x => x.UpdateBy), ct);
        return Ok(rows.Select(x => ToDto(x, userMap)).ToList());
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ProductionOrderDto>> GetById(long id, CancellationToken ct)
    {
        var row = await db.ProductionOrders
            .AsNoTracking()
            .Include(x => x.OutputProduct)
            .Include(x => x.MaterialLines).ThenInclude(x => x.MaterialProduct)
            .Include(x => x.WasteRecords).ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (row is null) return NotFound();

        var userMap = await ResolveUserMapAsync(new[] { row.UpdateBy }, ct);
        return Ok(ToDto(row, userMap));
    }

    [HttpPost]
    public async Task<ActionResult<ProductionOrderDto>> Create([FromBody] UpsertProductionOrderDto dto, CancellationToken ct)
    {
        var validation = await ValidateAsync(dto, null, ct);
        if (validation is not null) return BadRequest(validation);

        var entity = new ProductionOrder
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            OrderNo = dto.OrderNo.Trim(),
            OutputProductId = dto.OutputProductId,
            Status = ParseStatus(dto.Status),
            PlannedQuantity = dto.PlannedQuantity,
            ActualQuantity = dto.ActualQuantity,
            Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
        };

        foreach (var m in dto.Materials)
        {
            entity.MaterialLines.Add(new ProductionOrderMaterialLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                MaterialProductId = m.MaterialProductId,
                PlannedQuantity = m.PlannedQuantity,
                IssuedQuantity = m.IssuedQuantity,
                ReturnedQuantity = m.ReturnedQuantity
            });
        }
        foreach (var w in dto.Wastes)
        {
            entity.WasteRecords.Add(new ProductionWasteRecord
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ProductId = w.ProductId,
                PlannedQuantity = w.PlannedQuantity,
                ActualQuantity = w.ActualQuantity,
                WasteQuantity = w.WasteQuantity,
                Reason = string.IsNullOrWhiteSpace(w.Reason) ? null : w.Reason.Trim()
            });
        }

        db.ProductionOrders.Add(entity);
        await db.SaveChangesAsync(ct);
        var row = await db.ProductionOrders
            .AsNoTracking()
            .Include(x => x.OutputProduct)
            .Include(x => x.MaterialLines).ThenInclude(x => x.MaterialProduct)
            .Include(x => x.WasteRecords).ThenInclude(x => x.Product)
            .FirstAsync(x => x.Id == entity.Id, ct);
        var userMap = await ResolveUserMapAsync(new[] { row.UpdateBy }, ct);
        return Ok(ToDto(row, userMap));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, [FromBody] UpsertProductionOrderDto dto, CancellationToken ct)
    {
        var entity = await db.ProductionOrders
            .Include(x => x.MaterialLines)
            .Include(x => x.WasteRecords)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        var validation = await ValidateAsync(dto, id, ct);
        if (validation is not null) return BadRequest(validation);

        entity.OrderNo = dto.OrderNo.Trim();
        entity.OutputProductId = dto.OutputProductId;
        entity.Status = ParseStatus(dto.Status);
        entity.PlannedQuantity = dto.PlannedQuantity;
        entity.ActualQuantity = dto.ActualQuantity;
        entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

        db.ProductionOrderMaterialLines.RemoveRange(entity.MaterialLines);
        db.ProductionWasteRecords.RemoveRange(entity.WasteRecords);
        entity.MaterialLines.Clear();
        entity.WasteRecords.Clear();

        foreach (var m in dto.Materials)
        {
            entity.MaterialLines.Add(new ProductionOrderMaterialLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                MaterialProductId = m.MaterialProductId,
                PlannedQuantity = m.PlannedQuantity,
                IssuedQuantity = m.IssuedQuantity,
                ReturnedQuantity = m.ReturnedQuantity
            });
        }
        foreach (var w in dto.Wastes)
        {
            entity.WasteRecords.Add(new ProductionWasteRecord
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ProductId = w.ProductId,
                PlannedQuantity = w.PlannedQuantity,
                ActualQuantity = w.ActualQuantity,
                WasteQuantity = w.WasteQuantity,
                Reason = string.IsNullOrWhiteSpace(w.Reason) ? null : w.Reason.Trim()
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, CancellationToken ct)
    {
        var entity = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();
        db.ProductionOrders.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<string?> ValidateAsync(UpsertProductionOrderDto dto, long? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.OrderNo))
            return "工单号不能为空。";
        if (dto.PlannedQuantity <= 0)
            return "计划数量必须大于 0。";

        var duplicated = await db.ProductionOrders.AnyAsync(x => x.OrderNo == dto.OrderNo.Trim() && (!id.HasValue || x.Id != id.Value), ct);
        if (duplicated)
            return "工单号已存在。";

        var output = await db.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == dto.OutputProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (!output)
            return $"产出商品不存在：{dto.OutputProductId}";

        foreach (var m in dto.Materials)
        {
            var exists = await db.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == m.MaterialProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
            if (!exists) return $"物料不存在：{m.MaterialProductId}";
        }
        foreach (var w in dto.Wastes)
        {
            var exists = await db.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == w.ProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
            if (!exists) return $"损耗商品不存在：{w.ProductId}";
        }
        return null;
    }

    private static ProductionOrderStatus ParseStatus(string status)
        => Enum.TryParse<ProductionOrderStatus>(status, true, out var s) ? s : ProductionOrderStatus.Draft;

    private static string ToStatus(ProductionOrderStatus status) => status.ToString();

    private static ProductionOrderDto ToDto(ProductionOrder x, Dictionary<long, string?> userMap)
        => new(
            x.Id,
            x.OrderNo,
            x.OutputProductId,
            x.OutputProduct.Code,
            x.OutputProduct.Name,
            ToStatus(x.Status),
            x.PlannedQuantity,
            x.ActualQuantity,
            x.Notes,
            x.MaterialLines.Select(m => new ProductionOrderMaterialDto(
                m.Id, m.MaterialProductId, m.MaterialProduct.Code, m.MaterialProduct.Name, m.PlannedQuantity, m.IssuedQuantity, m.ReturnedQuantity
            )).ToList(),
            x.WasteRecords.Select(w => new ProductionWasteRecordDto(
                w.Id, w.ProductId, w.Product.Code, w.Product.Name, w.PlannedQuantity, w.ActualQuantity, w.WasteQuantity, w.Reason
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
