using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/production/inventory-transactions")]
[Authorize]
public sealed class InventoryTransactionsController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InventoryTransactionDto>>> List(
        [FromQuery] long? productId,
        [FromQuery] string? transactionType,
        CancellationToken ct)
    {
        var q = db.InventoryTransactions
            .AsNoTracking()
            .Include(x => x.Product)
            .AsQueryable();

        if (productId is { } pid)
            q = q.Where(x => x.ProductId == pid);

        if (!string.IsNullOrWhiteSpace(transactionType) &&
            Enum.TryParse<InventoryTransactionType>(transactionType.Trim(), true, out var t))
            q = q.Where(x => x.TransactionType == t);

        var rows = await q
            .OrderByDescending(x => x.OccurredAt)
            .Take(500)
            .ToListAsync(ct);

        var userMap = await ResolveUserMapAsync(rows.Select(x => x.UpdateBy), ct);
        return Ok(rows.Select(x => new InventoryTransactionDto(
            x.Id,
            x.TransactionType.ToString(),
            x.ProductId,
            x.Product.Code,
            x.Product.Name,
            x.Quantity,
            x.BeforeQuantity,
            x.AfterQuantity,
            x.SourceType.ToString(),
            x.SourceId,
            x.ReferenceNo,
            x.OccurredAt,
            x.ReferenceNo,
            x.UpdateTime,
            x.UpdateBy,
            x.UpdateBy is null ? null : userMap.GetValueOrDefault((long)x.UpdateBy.Value)
        )).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<InventoryTransactionDto>> Create([FromBody] CreateInventoryTransactionDto dto, CancellationToken ct)
    {
        if (!Enum.TryParse<InventoryTransactionType>(dto.TransactionType, true, out var txType))
            return BadRequest($"无效流水类型：{dto.TransactionType}");
        if (!Enum.TryParse<InventorySourceType>(dto.SourceType, true, out var sourceType))
            return BadRequest($"无效来源类型：{dto.SourceType}");

        var product = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == dto.ProductId && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (product is null)
            return BadRequest("商品不存在。");

        var entity = new InventoryTransaction
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            ProductId = dto.ProductId,
            TransactionType = txType,
            Quantity = dto.Quantity,
            BeforeQuantity = dto.BeforeQuantity,
            AfterQuantity = dto.AfterQuantity,
            SourceType = sourceType,
            SourceId = dto.SourceId,
            ReferenceNo = string.IsNullOrWhiteSpace(dto.ReferenceNo) ? null : dto.ReferenceNo.Trim(),
            OccurredAt = dto.OccurredAt ?? DateTime.UtcNow
        };
        db.InventoryTransactions.Add(entity);
        await db.SaveChangesAsync(ct);

        return Ok(new InventoryTransactionDto(
            entity.Id,
            entity.TransactionType.ToString(),
            entity.ProductId,
            product.Code,
            product.Name,
            entity.Quantity,
            entity.BeforeQuantity,
            entity.AfterQuantity,
            entity.SourceType.ToString(),
            entity.SourceId,
            entity.ReferenceNo,
            entity.OccurredAt,
            entity.ReferenceNo,
            entity.UpdateTime,
            entity.UpdateBy,
            null
        ));
    }

    private async Task<Dictionary<long, string?>> ResolveUserMapAsync(IEnumerable<int?> updateBys, CancellationToken ct)
    {
        var ids = updateBys.Where(x => x is not null).Select(x => (long)x!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<long, string?>();
        return await db.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName, ct);
    }
}
