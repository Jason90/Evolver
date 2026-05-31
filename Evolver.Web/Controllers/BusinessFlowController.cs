using Evolver.Core.Entities;
using Evolver.Core.MultiTenancy;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/business")]
[Authorize]
public sealed class BusinessFlowController(AppDbContext db, ITenantContext tenant) : ControllerBase
{
    private const string AmountScaleParamKey = "sales.amount.decimalPlaces";
    private const int DefaultAmountDecimalPlaces = 4;

    [HttpPost("sales-forecast")]
    public async Task<ActionResult<IReadOnlyList<SalesForecastLineDto>>> SalesForecast([FromBody] ForecastRequestDto req, CancellationToken ct)
    {
        var (from, to) = ResolveDateRange(req.FromDate, req.ToDate, "month");
        var q = db.SalesEntries.AsNoTracking()
            .Include(x => x.Market)
            .Include(x => x.Product)
            .Where(x => x.Date >= from && x.Date <= to);
        if (!string.IsNullOrWhiteSpace(req.Market))
            q = q.Where(x => x.Market.Name == req.Market.Trim());

        var grouped = await q.GroupBy(x => new { MarketName = x.Market.Name, x.ProductId, ProductCode = x.Product.Code, ProductName = x.Product.Name })
            .Select(g => new
            {
                Market = g.Key.MarketName,
                g.Key.ProductId,
                Code = g.Key.ProductCode,
                g.Key.ProductName,
                Historical = g.Sum(x => x.UnitsSold)
            })
            .ToListAsync(ct);

        var rows = grouped
            .GroupBy(x => x.Market)
            .SelectMany(g =>
            {
                var ordered = g.OrderByDescending(x => x.Historical).ToList();
                var bestSellerCount = Math.Max(1, (int)Math.Ceiling(ordered.Count * 0.3m));
                return ordered.Select((x, idx) =>
                {
                    var best = idx < bestSellerCount;
                    var delta = best ? 0.2m : -0.2m;
                    return new SalesForecastLineDto(
                        x.Market,
                        x.ProductId,
                        x.Code,
                        x.ProductName,
                        x.Historical,
                        best,
                        Math.Round(x.Historical * (1 + delta), 2),
                        delta * 100);
                });
            })
            .OrderBy(x => x.Market).ThenByDescending(x => x.ForecastUnits)
            .ToList();

        return Ok(rows);
    }

    [HttpPost("bom-requirements")]
    public async Task<ActionResult<IReadOnlyList<BomRequirementItemDto>>> BomRequirements([FromBody] BomRequirementRequestDto req, CancellationToken ct)
    {
        var plan = req.PlannedSales.Where(x => x.Quantity > 0).ToList();
        if (plan.Count == 0) return Ok(Array.Empty<BomRequirementItemDto>());

        var headers = await db.BomHeaders.AsNoTracking()
            .Include(x => x.Lines).ThenInclude(x => x.ComponentProduct)
            .Where(x => x.IsActive && plan.Select(p => p.ProductId).Contains(x.FinishedProductId))
            .ToListAsync(ct);

        var byProduct = headers.GroupBy(x => x.FinishedProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Version).First());

        var required = new Dictionary<long, (string code, string name, decimal qty)>();
        foreach (var p in plan)
        {
            if (!byProduct.TryGetValue(p.ProductId, out var h)) continue;
            foreach (var l in h.Lines)
            {
                var add = l.Quantity * p.Quantity;
                if (required.TryGetValue(l.ComponentProductId, out var cur))
                    required[l.ComponentProductId] = (cur.code, cur.name, cur.qty + add);
                else
                    required[l.ComponentProductId] = (l.ComponentProduct.Code, l.ComponentProduct.Name, add);
            }
        }

        var compIds = required.Keys.ToList();
        var stocks = await db.InventorySnapshots.AsNoTracking()
            .Where(x => compIds.Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.CurrentStock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, ct);

        var rows = required.Select(kv =>
        {
            var current = stocks.GetValueOrDefault(kv.Key, 0m);
            var shortage = Math.Max(0m, kv.Value.qty - current);
            return new BomRequirementItemDto(kv.Key, kv.Value.code, kv.Value.name, Math.Round(kv.Value.qty, 2), Math.Round(current, 2), Math.Round(shortage, 2));
        }).OrderByDescending(x => x.ShortageQty).ThenBy(x => x.ComponentCode).ToList();

        return Ok(rows);
    }

    [HttpGet("raw-material-stocks")]
    public async Task<ActionResult<IReadOnlyList<BomRequirementItemDto>>> RawMaterialStocks(CancellationToken ct)
    {
        var rows = await db.InventorySnapshots.AsNoTracking()
            .Include(x => x.Product)
            .OrderBy(x => x.Product.Code)
            .Select(x => new BomRequirementItemDto(
                x.ProductId, x.Product.Code, x.Product.Name, 0m, x.CurrentStock, 0m))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderSummaryDto>> CreatePurchaseOrder([FromBody] PurchaseOrderCreateRequestDto req, CancellationToken ct)
    {
        var lines = req.Lines.Where(x => x.SuggestPurchaseQty > 0).ToList();
        if (lines.Count == 0) return BadRequest("没有可采购的缺口数据。");

        var supplierCode = string.IsNullOrWhiteSpace(req.SupplierCode) ? null : req.SupplierCode.Trim();
        var supplierName = string.IsNullOrWhiteSpace(req.SupplierName) ? null : req.SupplierName.Trim();
        if (supplierCode is null)
        {
            var topProductId = lines.OrderByDescending(x => x.SuggestPurchaseQty).Select(x => x.ProductId).FirstOrDefault();
            var preferredSupplier = await db.PurchaseOrderLines.AsNoTracking()
                .Include(x => x.PurchaseOrder).ThenInclude(x => x.Supplier)
                .Where(x => x.ProductId == topProductId && x.PurchaseOrder.TenantId == tenant.TenantId && x.PurchaseOrder.OrgId == tenant.OrgId)
                .OrderByDescending(x => x.PurchaseOrder.Id)
                .Select(x => x.PurchaseOrder.Supplier)
                .FirstOrDefaultAsync(ct);
            if (preferredSupplier is not null)
            {
                supplierCode = preferredSupplier.Code;
                supplierName = preferredSupplier.Name;
            }
        }
        supplierCode ??= "AUTO-SUP-001";
        supplierName ??= "系统自动供应商";

        var supplier = await db.Suppliers.FirstOrDefaultAsync(
            x => x.Code == supplierCode && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.IsActive,
            ct);
        if (supplier is null)
        {
            supplier = new Supplier
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                Code = supplierCode,
                Name = supplierName
            };
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(ct);
        }

        var po = new PurchaseOrder
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            OrderNo = $"PO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            SupplierId = supplier.Id,
            Status = PurchaseOrderStatus.Draft,
            OrderDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ExpectedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            Notes = "由销售预测+BOM缺口自动生成"
        };
        db.PurchaseOrders.Add(po);
        await db.SaveChangesAsync(ct);

        foreach (var l in lines)
        {
            var bestUnitPrice = await db.PurchaseOrderLines.AsNoTracking()
                .Include(x => x.PurchaseOrder)
                .Where(x => x.ProductId == l.ProductId && x.PurchaseOrder.SupplierId == supplier.Id && x.PurchaseOrder.TenantId == tenant.TenantId && x.PurchaseOrder.OrgId == tenant.OrgId)
                .OrderByDescending(x => x.PurchaseOrder.Id)
                .Select(x => (decimal?)x.UnitPrice)
                .FirstOrDefaultAsync(ct);
            var finalUnitPrice = bestUnitPrice ?? l.UnitCost;

            db.PurchaseOrderLines.Add(new PurchaseOrderLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                PurchaseOrderId = po.Id,
                ProductId = l.ProductId,
                Quantity = l.SuggestPurchaseQty,
                UnitPrice = finalUnitPrice,
                LineAmount = l.SuggestPurchaseQty * finalUnitPrice,
                ReceivedQuantity = 0
            });
        }
        await db.SaveChangesAsync(ct);

        var total = await db.PurchaseOrderLines.AsNoTracking()
            .Where(x => x.PurchaseOrderId == po.Id)
            .SumAsync(x => x.LineAmount, ct);
        return Ok(new PurchaseOrderSummaryDto(po.Id, po.OrderNo, supplier.Name, po.Status.ToString(), po.OrderDate, Math.Round(total, 2)));
    }

    [HttpGet("purchase-orders")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderSummaryDto>>> ListPurchaseOrders(CancellationToken ct)
    {
        var rows = await db.PurchaseOrders.AsNoTracking()
            .Include(x => x.Supplier)
            .Include(x => x.Lines)
            .OrderByDescending(x => x.Id)
            .Take(200)
            .Select(x => new PurchaseOrderSummaryDto(
                x.Id,
                x.OrderNo,
                x.Supplier.Name,
                x.Status.ToString(),
                x.OrderDate,
                x.Lines.Sum(l => l.LineAmount)))
            .ToListAsync(ct);
        return Ok(rows);
    }

    [HttpPost("purchase-orders/{id:long}/receive")]
    public async Task<ActionResult<PurchaseReceiveResultDto>> ReceivePurchaseOrder(long id, [FromBody] PurchaseReceiveRequestDto? req, CancellationToken ct)
    {
        var po = await db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Lines).ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (po is null) return NotFound();

        var receiveMap = req?.Lines?
            .Where(x => x.ReceiveQuantity > 0)
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ReceiveQuantity));

        decimal receivedAmount = 0m;
        foreach (var line in po.Lines)
        {
            var pending = Math.Max(0m, line.Quantity - line.ReceivedQuantity);
            var targetReceive = receiveMap is null
                ? pending
                : Math.Min(pending, Math.Max(0m, receiveMap.GetValueOrDefault(line.ProductId, 0m)));

            if (receiveMap is not null && targetReceive <= 0) continue;
            pending = targetReceive;
            if (pending <= 0) continue;

            line.ReceivedQuantity += pending;
            receivedAmount += pending * line.UnitPrice;

            var snapshot = await db.InventorySnapshots.FirstOrDefaultAsync(x => x.ProductId == line.ProductId && x.LocationCode == "", ct);
            if (snapshot is null)
            {
                snapshot = new InventorySnapshot
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    ProductId = line.ProductId,
                    LocationCode = "",
                    CurrentStock = 0m,
                    SafetyStock = 0m,
                    LastUpdateTime = DateTime.UtcNow
                };
                db.InventorySnapshots.Add(snapshot);
                await db.SaveChangesAsync(ct);
            }

            var before = snapshot.CurrentStock;
            snapshot.CurrentStock += pending;
            snapshot.LastUpdateTime = DateTime.UtcNow;

            db.InventoryTransactions.Add(new InventoryTransaction
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ProductId = line.ProductId,
                TransactionType = InventoryTransactionType.Inbound,
                Quantity = pending,
                BeforeQuantity = before,
                AfterQuantity = snapshot.CurrentStock,
                SourceType = InventorySourceType.PurchaseOrder,
                SourceId = po.Id,
                ReferenceNo = $"PO-IN:{po.OrderNo}",
                OccurredAt = DateTime.UtcNow
            });
        }

        var hasPending = po.Lines.Any(x => x.ReceivedQuantity < x.Quantity);
        po.Status = hasPending ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Received;

        var ap = await db.AccountsPayables.FirstOrDefaultAsync(x => x.PurchaseOrderId == po.Id, ct);
        if (ap is null)
        {
            ap = new AccountsPayable
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                DocumentNo = $"AP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                SupplierId = po.SupplierId,
                PurchaseOrderId = po.Id,
                Amount = po.Lines.Sum(x => x.ReceivedQuantity * x.UnitPrice),
                SettledAmount = 0m,
                DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
                Status = FinanceDocumentStatus.Open
            };
            db.AccountsPayables.Add(ap);
        }
        else
        {
            ap.Amount = po.Lines.Sum(x => x.ReceivedQuantity * x.UnitPrice);
        }

        await db.SaveChangesAsync(ct);
        return Ok(new PurchaseReceiveResultDto(po.Id, po.OrderNo, Math.Round(receivedAmount, 2), po.Status.ToString(), ap.DocumentNo));
    }

    [HttpPost("sales-orders/simulate-material-check")]
    public async Task<ActionResult<IReadOnlyList<MaterialShortageDto>>> SimulateMaterialCheck([FromBody] SalesOrderCreateRequestDto req, CancellationToken ct)
    {
        var lines = req.Lines.Where(x => x.Quantity > 0).ToList();
        if (lines.Count == 0) return Ok(Array.Empty<MaterialShortageDto>());

        var materialRequirements = await BuildMaterialRequirementsAsync(lines, ct);
        var stockMap = await db.InventorySnapshots
            .Where(x => materialRequirements.Select(m => m.ProductId).Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.CurrentStock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, ct);

        var productIds = materialRequirements.Select(x => x.ProductId).ToList();
        var costMap = await db.Products.AsNoTracking()
            .Where(x => productIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UnitCost ?? 0m, ct);

        var shortages = materialRequirements
            .Select(x =>
            {
                var available = stockMap.GetValueOrDefault(x.ProductId, 0m);
                var shortQty = Math.Max(0m, x.RequiredQty - available);
                return new MaterialShortageDto(x.ProductId, x.ProductCode, x.ProductName, x.RequiredQty, available, shortQty, costMap.GetValueOrDefault(x.ProductId, 0m));
            })
            .Where(x => x.ShortageQty > 0)
            .OrderByDescending(x => x.ShortageQty)
            .ToList();
        return Ok(shortages);
    }

    [HttpPost("sales-orders")]
    public async Task<ActionResult<SalesOrderResultDto>> CreateSalesOrder([FromBody] SalesOrderCreateRequestDto req, CancellationToken ct)
    {
        var amountScale = await GetAmountDecimalPlacesAsync(ct);
        var lines = req.Lines.Where(x => x.Quantity > 0).ToList();
        if (lines.Count == 0) return BadRequest("销售明细不能为空。");
        var pricedLines = lines
            .Select(x =>
            {
                var unitPrice = Math.Round(x.UnitPrice, amountScale);
                return new
                {
                    x.ProductId,
                    x.Quantity,
                    UnitPrice = unitPrice,
                    LineAmount = Math.Round(x.Quantity * unitPrice, amountScale)
                };
            })
            .ToList();

        var materialRequirements = await BuildMaterialRequirementsAsync(lines, ct);
        var stockMap = await db.InventorySnapshots
            .Where(x => materialRequirements.Select(m => m.ProductId).Contains(x.ProductId))
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Stock = g.Sum(x => x.CurrentStock) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Stock, ct);
        var materialIds = materialRequirements.Select(x => x.ProductId).ToList();
        var costMap = await db.Products.AsNoTracking()
            .Where(x => materialIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UnitCost ?? 0m, ct);
        var shortages = materialRequirements
            .Select(x =>
            {
                var available = stockMap.GetValueOrDefault(x.ProductId, 0m);
                var shortQty = Math.Max(0m, x.RequiredQty - available);
                return new MaterialShortageDto(x.ProductId, x.ProductCode, x.ProductName, x.RequiredQty, available, shortQty, costMap.GetValueOrDefault(x.ProductId, 0m));
            })
            .Where(x => x.ShortageQty > 0)
            .OrderByDescending(x => x.ShortageQty)
            .ToList();

        if (req.EnforceMaterialCheck && shortages.Count > 0)
            return BadRequest($"原材料库存不足，缺口 {shortages.Count} 项。");

        var marketName = string.IsNullOrWhiteSpace(req.MarketName) ? "默认市场" : req.MarketName.Trim();
        var customerName = string.IsNullOrWhiteSpace(req.CustomerName) ? "散客" : req.CustomerName.Trim();

        var market = await db.Markets.FirstOrDefaultAsync(
            x => x.Name == marketName && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId && x.IsActive,
            ct);
        if (market is null)
        {
            market = new Market { TenantId = tenant.TenantId, OrgId = tenant.OrgId, Name = marketName };
            db.Markets.Add(market);
            await db.SaveChangesAsync(ct);
        }

        var customer = await db.Customers.FirstOrDefaultAsync(x => x.Name == customerName && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (customer is null)
        {
            customer = new Customer
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                Code = $"CUST-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Name = customerName
            };
            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);
        }

        var salesUser = !string.IsNullOrWhiteSpace(req.SalesUserName)
            ? await db.Users.FirstOrDefaultAsync(x => x.UserName == req.SalesUserName && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct)
            : await db.Users.FirstOrDefaultAsync(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (salesUser is null) return BadRequest("未找到可用销售用户。");

        var subTotal = Math.Round(pricedLines.Sum(x => x.LineAmount), amountScale);
        var so = new SalesOrder
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            OrderNo = $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerId = customer.Id,
            SalesUserId = salesUser.Id,
            Status = req.CheckoutNow ? SalesOrderStatus.Completed : SalesOrderStatus.PendingProduction,
            OrderTime = DateTime.UtcNow,
            Notes = req.Notes,
            SubTotal = subTotal,
            TaxAmount = 0,
            TotalAmount = subTotal
        };
        db.SalesOrders.Add(so);
        await db.SaveChangesAsync(ct);

        foreach (var l in pricedLines)
        {
            db.SalesOrderLines.Add(new SalesOrderLine
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                SalesOrderId = so.Id,
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                LineAmount = l.LineAmount
            });

            db.SalesEntries.Add(new SalesEntry
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                MarketId = market.Id,
                ProductId = l.ProductId,
                UnitsSold = l.Quantity,
                UnitPrice = l.UnitPrice,
                SalesValue = l.LineAmount,
                Notes = "来自销售单自动记录"
            });
        }

        db.OrderOperationLogs.Add(new OrderOperationLog
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            SalesOrderId = so.Id,
            Action = req.CheckoutNow ? "CreateAndCheckout" : "Create",
            FromStatus = null,
            ToStatus = so.Status,
            ActorUserId = salesUser.Id,
            Detail = "自动创建销售单"
        });

        db.AccountsReceivables.Add(new AccountsReceivable
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            DocumentNo = $"AR-{DateTime.UtcNow:yyyyMMddHHmmss}",
            CustomerId = customer.Id,
            SalesOrderId = so.Id,
            Amount = so.TotalAmount,
            SettledAmount = req.CheckoutNow ? so.TotalAmount : 0m,
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            Status = req.CheckoutNow ? FinanceDocumentStatus.Settled : FinanceDocumentStatus.Open
        });
        await db.SaveChangesAsync(ct);

        long? prodId = null;
        string? prodNo = null;
        var canReserveMaterials = shortages.Count == 0;
        foreach (var l in lines)
        {
            var header = await db.BomHeaders.AsNoTracking()
                .Include(x => x.Lines)
                .Where(x => x.FinishedProductId == l.ProductId && x.IsActive)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(ct);
            if (header is null) continue;

            var po = new ProductionOrder
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                OrderNo = $"MO-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                OutputProductId = l.ProductId,
                SourceSalesOrderId = so.Id,
                PlannedQuantity = l.Quantity,
                ActualQuantity = 0,
                Status = ProductionOrderStatus.Released,
                Notes = "由销售单自动生成"
            };
            db.ProductionOrders.Add(po);
            await db.SaveChangesAsync(ct);

            foreach (var bl in header.Lines)
            {
                var requiredQty = bl.Quantity * l.Quantity;
                db.ProductionOrderMaterialLines.Add(new ProductionOrderMaterialLine
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    ProductionOrderId = po.Id,
                    MaterialProductId = bl.ComponentProductId,
                    PlannedQuantity = requiredQty,
                    IssuedQuantity = canReserveMaterials ? requiredQty : 0,
                    ReturnedQuantity = 0
                });

                if (canReserveMaterials)
                    await ConsumeInventoryAsync(bl.ComponentProductId, requiredQty, $"MO:{po.OrderNo}", ct);
            }
            await db.SaveChangesAsync(ct);

            prodId ??= po.Id;
            prodNo ??= po.OrderNo;
        }

        return Ok(new SalesOrderResultDto(so.Id, so.OrderNo, so.Status.ToString(), so.TotalAmount, prodId, prodNo, shortages));
    }

    [HttpGet("sales-orders")]
    public async Task<ActionResult<IReadOnlyList<SalesOrderSummaryDto>>> ListSalesOrders(CancellationToken ct)
    {
        var rowsRaw = await db.SalesOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderByDescending(x => x.Id)
            .Take(200)
            .Select(x => new
            {
                x.Id,
                x.OrderNo,
                CustomerName = x.Customer.Name,
                OrderStatus = x.Status == SalesOrderStatus.PendingProduction ? "新单" : x.Status.ToString(),
                OrderDate = DateOnly.FromDateTime(x.OrderTime),
                SalesUserName = x.SalesUser.UserName ?? "-",
                PaymentStatus = db.AccountsReceivables
                    .Where(ar => ar.SalesOrderId == x.Id)
                    .Select(ar => ar.Status == FinanceDocumentStatus.Settled
                        ? "完全结算"
                        : ar.Status == FinanceDocumentStatus.PartiallySettled
                            ? "部分结算"
                            : "未结算")
                    .FirstOrDefault() ?? "未结算",
                TotalQuantityRaw = x.Lines.Select(l => (double?)l.Quantity).Sum() ?? 0d,
                x.TotalAmount,
                ReceivedAmount = db.AccountsReceivables.Where(ar => ar.SalesOrderId == x.Id).Select(ar => ar.SettledAmount).FirstOrDefault(),
                x.Notes,
                UpdateByUserName = db.Users.Where(u => x.UpdateBy != null && u.Id == x.UpdateBy.Value).Select(u => u.UserName).FirstOrDefault(),
                x.UpdateTime
            })
            .ToListAsync(ct);
        var rows = rowsRaw.Select(x => new SalesOrderSummaryDto(
            x.Id,
            x.OrderNo,
            x.CustomerName,
            x.OrderStatus,
            x.OrderDate,
            x.SalesUserName,
            x.PaymentStatus,
            Convert.ToDecimal(x.TotalQuantityRaw),
            x.TotalAmount,
            x.ReceivedAmount,
            x.Notes,
            x.UpdateByUserName,
            x.UpdateTime)).ToList();
        return Ok(rows);
    }

    [HttpGet("sales-orders/{id:long}")]
    public async Task<ActionResult<SalesOrderDetailDto>> GetSalesOrderDetail(long id, CancellationToken ct)
    {
        var row = await db.SalesOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .Select(x => new
            {
                x.Id,
                x.OrderNo,
                CustomerName = x.Customer.Name,
                x.Status,
                x.OrderTime,
                SalesUserName = x.SalesUser.UserName ?? "-",
                TotalQuantityRaw = x.Lines.Select(l => (double?)l.Quantity).Sum() ?? 0d,
                x.TotalAmount,
                ReceivedAmount = db.AccountsReceivables.Where(ar => ar.SalesOrderId == x.Id).Select(ar => ar.SettledAmount).FirstOrDefault(),
                PaymentStatus = db.AccountsReceivables
                    .Where(ar => ar.SalesOrderId == x.Id)
                    .Select(ar => ar.Status == FinanceDocumentStatus.Settled
                        ? "完全结算"
                        : ar.Status == FinanceDocumentStatus.PartiallySettled
                            ? "部分结算"
                            : "未结算")
                    .FirstOrDefault() ?? "未结算",
                x.Notes,
                x.UpdateBy,
                x.UpdateTime
            })
            .FirstOrDefaultAsync(ct);
        if (row is null) return NotFound();

        var lines = await db.SalesOrderLines
            .AsNoTracking()
            .Where(x => x.SalesOrderId == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .OrderBy(x => x.Id)
            .Select(x => new SalesOrderLineDetailDto(
                x.ProductId,
                x.Product.Code,
                x.Product.Name,
                x.Product.UnitCost,
                x.Product.SuggestedPrice,
                x.Quantity,
                x.PointsUsed ?? 100m,
                x.UnitPrice,
                x.LineAmount,
                null))
            .ToListAsync(ct);

        var updateByName = row.UpdateBy is null
            ? null
            : await db.Users.AsNoTracking()
                .Where(u => u.Id == row.UpdateBy.Value)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct);

        return Ok(new SalesOrderDetailDto(
            row.Id,
            row.OrderNo,
            row.CustomerName,
            row.Status == SalesOrderStatus.PendingProduction ? "新单" : row.Status.ToString(),
            DateOnly.FromDateTime(row.OrderTime),
            row.SalesUserName,
            row.PaymentStatus,
            Convert.ToDecimal(row.TotalQuantityRaw),
            row.TotalAmount,
            row.ReceivedAmount,
            row.Notes,
            updateByName,
            row.UpdateTime,
            lines));
    }

    [HttpGet("sales-orders/finished-products")]
    public async Task<ActionResult<IReadOnlyList<SalesOrderProductOptionDto>>> ListFinishedProducts(CancellationToken ct)
    {
        var rows = await db.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                x.UnitCost,
                x.SuggestedPrice,
                CategoryCode = x.Category == null ? "" : x.Category.Code,
                CategoryName = x.Category == null ? "" : x.Category.Name
            })
            .ToListAsync(ct);

        bool IsFinished(dynamic x)
        {
            var code = ((string)x.CategoryCode ?? "").Trim();
            var name = ((string)x.CategoryName ?? "").Trim();
            if (code.Equals("Finished", StringComparison.OrdinalIgnoreCase)) return true;
            if (code.Equals("FG", StringComparison.OrdinalIgnoreCase)) return true;
            if (name.Contains("成品", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        var filtered = rows.Where(IsFinished).ToList();
        if (filtered.Count == 0)
            filtered = rows;

        return Ok(filtered
            .OrderBy(x => x.Code)
            .Select(x => new SalesOrderProductOptionDto(x.Id, x.Code, x.Name, x.UnitCost, x.SuggestedPrice))
            .ToList());
    }

    [HttpGet("sales-orders/amount-scale")]
    public async Task<ActionResult<SalesOrderAmountScaleDto>> GetSalesOrderAmountScale(CancellationToken ct)
    {
        var value = await GetAmountDecimalPlacesAsync(ct);
        return Ok(new SalesOrderAmountScaleDto(value));
    }

    [HttpPost("sales-orders/{id:long}/status")]
    public async Task<ActionResult> ChangeSalesOrderStatus(long id, [FromQuery] string toStatus, CancellationToken ct)
    {
        if (!Enum.TryParse<SalesOrderStatus>(toStatus, true, out var target))
            return BadRequest("无效状态。");
        var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (so is null) return NotFound();
        var from = so.Status;
        so.Status = target;
        db.OrderOperationLogs.Add(new OrderOperationLog
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            SalesOrderId = so.Id,
            Action = "StatusTransition",
            FromStatus = from,
            ToStatus = target,
            Detail = $"状态流转：{from} -> {target}"
        });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("production-orders/{id:long}/complete")]
    public async Task<ActionResult> CompleteProductionOrder(long id, [FromQuery] decimal actualQty, CancellationToken ct)
    {
        if (actualQty <= 0) return BadRequest("实际产量必须大于 0。");
        var po = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (po is null) return NotFound();
        var from = po.Status;
        po.ActualQuantity = actualQty;
        po.Status = ProductionOrderStatus.Completed;
        await ConsumeInventoryAsync(po.OutputProductId, -actualQty, $"MO-OUT:{po.OrderNo}", ct);

        if (po.SourceSalesOrderId is long soId)
        {
            var so = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == soId, ct);
            if (so is not null && so.Status == SalesOrderStatus.PendingProduction)
                so.Status = SalesOrderStatus.PendingDispatch;
            if (so is not null)
            {
                db.OrderOperationLogs.Add(new OrderOperationLog
                {
                    TenantId = tenant.TenantId,
                    OrgId = tenant.OrgId,
                    SalesOrderId = so.Id,
                    Action = "ProductionCompleted",
                    FromStatus = so.Status,
                    ToStatus = so.Status,
                    Detail = $"生产单 {po.OrderNo} 完工，状态 {from}->{po.Status}"
                });
            }
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("production-orders/{id:long}/status")]
    public async Task<ActionResult> ChangeProductionOrderStatus(long id, [FromQuery] string toStatus, CancellationToken ct)
    {
        if (!Enum.TryParse<ProductionOrderStatus>(toStatus, true, out var status))
            return BadRequest("无效状态。");
        var po = await db.ProductionOrders.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenant.TenantId && x.OrgId == tenant.OrgId, ct);
        if (po is null) return NotFound();
        po.Status = status;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("operations-analysis")]
    public async Task<ActionResult<IReadOnlyList<OperationsAnalysisRowDto>>> OperationsAnalysis([FromBody] OperationsAnalysisQueryDto req, CancellationToken ct)
    {
        var (from, to) = ResolveDateRange(req.FromDate, req.ToDate, req.Range);
        var laborCost = await db.OperatingCostEntries.AsNoTracking()
            .Where(x => x.CostDate >= from && x.CostDate <= to && x.Category == OperatingCostCategory.Labor)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;

        var groupBy = (req.GroupBy ?? "product").Trim().ToLowerInvariant();
        if (groupBy == "market")
        {
            var marketRows = await db.SalesEntries.AsNoTracking()
                .Include(x => x.Product)
                .Include(x => x.Market)
                .Where(x => x.Date >= from && x.Date <= to)
                .GroupBy(x => x.Market.Name)
                .Select(g => new
                {
                    Key = g.Key,
                    Qty = g.Sum(x => x.UnitsSold),
                    Amount = g.Sum(x => x.SalesValue),
                    Cost = g.Sum(x => x.UnitsSold * (x.Product.UnitCost ?? 0m))
                })
                .ToListAsync(ct);
            return Ok(marketRows.Select(x => ToAnalysisRow(x.Key, x.Qty, x.Amount, x.Cost, laborCost)).ToList());
        }

        var lines = db.SalesOrderLines.AsNoTracking()
            .Include(x => x.SalesOrder).ThenInclude(x => x.Customer)
            .Include(x => x.SalesOrder).ThenInclude(x => x.SalesUser)
            .Include(x => x.Product).ThenInclude(x => x.Category)
            .Where(x => DateOnly.FromDateTime(x.SalesOrder.OrderTime) >= from && DateOnly.FromDateTime(x.SalesOrder.OrderTime) <= to);

        var data = await lines.Select(x => new
        {
            Customer = x.SalesOrder.Customer.Name,
            User = x.SalesOrder.SalesUser.UserName ?? "-",
            Product = x.Product.Code,
            Category = x.Product.Category != null ? x.Product.Category.Code : "-",
            Qty = x.Quantity,
            Amount = x.LineAmount,
            Cost = x.Quantity * (x.Product.UnitCost ?? 0m)
        }).ToListAsync(ct);

        Func<dynamic, string> keySelector = groupBy switch
        {
            "customer" => r => (string)r.Customer,
            "user" => r => (string)r.User,
            "category" => r => (string)r.Category,
            _ => r => (string)r.Product
        };

        var result = data.GroupBy(keySelector)
            .Select(g => ToAnalysisRow(g.Key, g.Sum(x => (decimal)x.Qty), g.Sum(x => (decimal)x.Amount), g.Sum(x => (decimal)x.Cost), laborCost))
            .OrderByDescending(x => x.SalesAmount)
            .ToList();

        return Ok(result);
    }

    private static OperationsAnalysisRowDto ToAnalysisRow(string key, decimal qty, decimal amount, decimal cost, decimal laborCost)
    {
        var profitRate = amount <= 0 ? 0 : (amount - cost) / amount;
        var laborRate = amount <= 0 ? 0 : laborCost / amount;
        var salesCostRate = amount <= 0 ? 0 : cost / amount;
        return new OperationsAnalysisRowDto(key, qty, amount, Math.Round(profitRate, 4), Math.Round(laborRate, 4), Math.Round(salesCostRate, 4));
    }

    private async Task<List<(long ProductId, string ProductCode, string ProductName, decimal RequiredQty)>> BuildMaterialRequirementsAsync(
        IReadOnlyList<SalesOrderCreateLineDto> lines,
        CancellationToken ct)
    {
        var requirements = new Dictionary<long, (string code, string name, decimal qty)>();
        foreach (var line in lines)
        {
            var header = await db.BomHeaders.AsNoTracking()
                .Include(x => x.Lines).ThenInclude(x => x.ComponentProduct)
                .Where(x => x.FinishedProductId == line.ProductId && x.IsActive)
                .OrderByDescending(x => x.Version)
                .FirstOrDefaultAsync(ct);
            if (header is null) continue;

            foreach (var bl in header.Lines)
            {
                var need = bl.Quantity * line.Quantity;
                if (requirements.TryGetValue(bl.ComponentProductId, out var current))
                    requirements[bl.ComponentProductId] = (current.code, current.name, current.qty + need);
                else
                    requirements[bl.ComponentProductId] = (bl.ComponentProduct.Code, bl.ComponentProduct.Name, need);
            }
        }
        return requirements.Select(x => (x.Key, x.Value.code, x.Value.name, Math.Round(x.Value.qty, 2))).ToList();
    }

    private async Task ConsumeInventoryAsync(long productId, decimal deltaQty, string referenceNo, CancellationToken ct)
    {
        var snapshot = await db.InventorySnapshots.FirstOrDefaultAsync(x => x.ProductId == productId && x.LocationCode == "", ct);
        if (snapshot is null)
        {
            snapshot = new InventorySnapshot
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                ProductId = productId,
                LocationCode = "",
                CurrentStock = 0m,
                SafetyStock = 0m,
                LastUpdateTime = DateTime.UtcNow
            };
            db.InventorySnapshots.Add(snapshot);
            await db.SaveChangesAsync(ct);
        }

        var before = snapshot.CurrentStock;
        snapshot.CurrentStock -= deltaQty;
        snapshot.LastUpdateTime = DateTime.UtcNow;

        db.InventoryTransactions.Add(new InventoryTransaction
        {
            TenantId = tenant.TenantId,
            OrgId = tenant.OrgId,
            ProductId = productId,
            TransactionType = deltaQty >= 0 ? InventoryTransactionType.ProductionConsume : InventoryTransactionType.ProductionOutput,
            Quantity = Math.Abs(deltaQty),
            BeforeQuantity = before,
            AfterQuantity = snapshot.CurrentStock,
            SourceType = InventorySourceType.ProductionOrder,
            ReferenceNo = referenceNo,
            OccurredAt = DateTime.UtcNow
        });
    }

    private static (DateOnly from, DateOnly to) ResolveDateRange(DateOnly? from, DateOnly? to, string? range)
    {
        if (from.HasValue && to.HasValue) return (from.Value, to.Value);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var r = (range ?? "month").Trim().ToLowerInvariant();
        return r switch
        {
            "day" => (today, today),
            "week" => (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)), today),
            "year" => (new DateOnly(today.Year, 1, 1), today),
            "all" or "total" => (new DateOnly(2000, 1, 1), today),
            _ => (new DateOnly(today.Year, today.Month, 1), today)
        };
    }

    private async Task<int> GetAmountDecimalPlacesAsync(CancellationToken ct)
    {
        var param = await db.SystemParameters
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x =>
                x.TenantId == tenant.TenantId &&
                x.OrgId == tenant.OrgId &&
                x.ParamKey == AmountScaleParamKey, ct);

        if (param is null)
        {
            db.SystemParameters.Add(new SystemParameter
            {
                TenantId = tenant.TenantId,
                OrgId = tenant.OrgId,
                Name = "销售金额小数位数",
                ParamKey = AmountScaleParamKey,
                ParamValue = DefaultAmountDecimalPlaces.ToString(),
                IsSystemBuiltIn = true,
                Remark = "销售订单金额和单价保留小数位数",
                IsActive = true
            });
            await db.SaveChangesAsync(ct);
            return DefaultAmountDecimalPlaces;
        }

        if (!int.TryParse(param.ParamValue, out var value))
            return DefaultAmountDecimalPlaces;
        return Math.Clamp(value, 0, 8);
    }
}
