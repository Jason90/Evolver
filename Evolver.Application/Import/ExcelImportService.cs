using System.Globalization;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Evolver.Core.Entities;
using Evolver.Infrastructure.Persistence;
using Evolver.Shared;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evolver.Application.Import;

public sealed class ExcelImportService(AppDbContext db, ILogger<ExcelImportService> logger)
{
    private static readonly Regex ProductCodeRegex = new(@"\((?<code>[^)]+)\)\s*$", RegexOptions.Compiled);

    public Task<ImportResultDto> ImportAsync(Stream xlsxStream, DateOnly? asOfDate = null, CancellationToken ct = default)
    {
        var date = asOfDate ?? DateOnly.FromDateTime(DateTime.Today);
        return ImportCoreAsync(xlsxStream, date, ct);
    }

    private async Task<ImportResultDto> ImportCoreAsync(Stream xlsxStream, DateOnly date, CancellationToken ct)
    {
        using var wb = new XLWorkbook(xlsxStream);

        var productsUpserted = await ImportPriceTableAsync(wb, ct);
        var (marketsUpserted, salesUpserted) = await ImportSalesLogAsync(wb, ct);
        var menuUpserted = await ImportMenuIntelligenceAsync(wb, date, ct);

        return new ImportResultDto(
            ProductsUpserted: productsUpserted,
            MarketsUpserted: marketsUpserted,
            SalesEntriesUpserted: salesUpserted,
            MenuIntelligenceRecordsUpserted: menuUpserted,
            InventorySnapshotsUpserted: 0
        );
    }

    private async Task<int> ImportPriceTableAsync(XLWorkbook wb, CancellationToken ct)
    {
        if (!wb.TryGetWorksheet("Price_Table", out var ws))
        {
            logger.LogWarning("Price_Table sheet missing");
            return 0;
        }

        var rows = ws.RangeUsed()?.RowsUsed().Skip(1).ToList() ?? [];
        var existing = await db.Products.Where(x => x.TenantId == Defaults.TenantId).ToListAsync(ct);
        var byCode = existing.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var upserted = 0;
        foreach (var r in rows)
        {
            var rawProduct = r.Cell(1).GetString().Trim();
            var rawPrice = r.Cell(2).GetString().Trim();

            if (string.IsNullOrWhiteSpace(rawProduct))
                continue;

            if (!decimal.TryParse(rawPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) &&
                !decimal.TryParse(rawPrice, NumberStyles.Any, CultureInfo.CurrentCulture, out price))
            {
                continue;
            }

            var (name, code) = ParseProduct(rawProduct);
            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!byCode.TryGetValue(code, out var p))
            {
                p = new Product
                {
                    TenantId = Defaults.TenantId,
                    OrgId = Defaults.OrgId,
                    Code = code,
                    Name = name,
                    UnitPrice = price,
                };
                db.Products.Add(p);
                byCode[code] = p;
            }
            else
            {
                p.Name = name;
                p.UnitPrice = price;
            }

            upserted++;
        }

        await EnsureDefaultTenantAsync(ct);
        await db.SaveChangesAsync(ct);
        return upserted;
    }

    private async Task<(int marketsUpserted, int salesUpserted)> ImportSalesLogAsync(XLWorkbook wb, CancellationToken ct)
    {
        if (!wb.TryGetWorksheet("Sales_Log", out var ws))
        {
            logger.LogWarning("Sales_Log sheet missing");
            return (0, 0);
        }

        var used = ws.RangeUsed();
        if (used is null)
            return (0, 0);

        var rows = used.RowsUsed().Skip(1).ToList();

        var products = await db.Products.Where(x => x.TenantId == Defaults.TenantId).ToListAsync(ct);
        var productByCode = products.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var markets = await db.Markets.Where(x => x.TenantId == Defaults.TenantId).ToListAsync(ct);
        var marketByName = markets.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

        var marketsUpserted = 0;
        var salesInserted = 0;

        foreach (var r in rows)
        {
            var dateCell = r.Cell(1);
            DateTime dateTime;
            if (dateCell.TryGetValue(out dateTime))
            {
                // ok
            }
            else if (!DateTime.TryParse(dateCell.GetString().Trim(), CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out dateTime) &&
                     !DateTime.TryParse(dateCell.GetString().Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dateTime))
            {
                continue;
            }
            var marketName = r.Cell(3).GetString().Trim();
            var rawProduct = r.Cell(4).GetString().Trim();
            var units = r.Cell(5).GetValue<decimal>();
            var unitPrice = r.Cell(6).GetValue<decimal>();
            var salesValue = r.Cell(7).GetValue<decimal>();
            var notes = r.Cell(8).GetString();

            if (string.IsNullOrWhiteSpace(marketName) || string.IsNullOrWhiteSpace(rawProduct))
                continue;

            var date = DateOnly.FromDateTime(dateTime);
            var (name, code) = ParseProduct(rawProduct);
            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!marketByName.TryGetValue(marketName, out var market))
            {
                market = new Market
                {
                    TenantId = Defaults.TenantId,
                    OrgId = Defaults.OrgId,
                    Name = marketName
                };
                db.Markets.Add(market);
                marketByName[marketName] = market;
                marketsUpserted++;
            }

            if (!productByCode.TryGetValue(code, out var product))
            {
                product = new Product
                {
                    TenantId = Defaults.TenantId,
                    OrgId = Defaults.OrgId,
                    Code = code,
                    Name = name,
                    UnitPrice = unitPrice
                };
                db.Products.Add(product);
                productByCode[code] = product;
            }

            var existing = await db.SalesEntries
                .Where(x => x.TenantId == Defaults.TenantId && x.MarketId == market.Id && x.Date == date && x.ProductId == product.Id)
                .ToListAsync(ct);
            if (existing.Count != 0)
                db.SalesEntries.RemoveRange(existing);

            db.SalesEntries.Add(new SalesEntry
            {
                TenantId = Defaults.TenantId,
                OrgId = Defaults.OrgId,
                Date = date,
                Market = market,
                Product = product,
                UnitsSold = units,
                UnitPrice = unitPrice,
                SalesValue = salesValue,
                Notes = notes
            });
            salesInserted++;
        }

        await EnsureDefaultTenantAsync(ct);
        await db.SaveChangesAsync(ct);
        return (marketsUpserted, salesInserted);
    }

    private async Task<int> ImportMenuIntelligenceAsync(XLWorkbook wb, DateOnly asOfDate, CancellationToken ct)
    {
        if (!wb.TryGetWorksheet("Menu_Intelligence", out var ws))
        {
            logger.LogWarning("Menu_Intelligence sheet missing");
            return 0;
        }

        string? selectedMarket = null;
        var used = ws.RangeUsed();
        if (used is null)
            return 0;

        foreach (var row in used.RowsUsed())
        {
            var c1 = row.Cell(1).GetString().Trim();
            if (string.Equals(c1, "Selected Market", StringComparison.OrdinalIgnoreCase))
            {
                selectedMarket = row.Cell(2).GetString().Trim();
                break;
            }
        }

        selectedMarket ??= "Unknown Market";

        var market = await db.Markets.FirstOrDefaultAsync(
            x => x.TenantId == Defaults.TenantId && x.Name == selectedMarket, ct);
        if (market is null)
        {
            market = new Market
            {
                TenantId = Defaults.TenantId,
                OrgId = Defaults.OrgId,
                Name = selectedMarket
            };
            db.Markets.Add(market);
        }

        int? headerRowNumber = null;
        foreach (var row in used.RowsUsed())
        {
            if (string.Equals(row.Cell(1).GetString().Trim(), "Rank", StringComparison.OrdinalIgnoreCase))
            {
                headerRowNumber = row.RowNumber();
                break;
            }
        }

        if (headerRowNumber is null)
            return 0;

        var products = await db.Products.Where(x => x.TenantId == Defaults.TenantId).ToListAsync(ct);
        var productByName = products.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var productByCode = products.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        var existingForDate = await db.MenuIntelligenceRecords
            .Where(x => x.TenantId == Defaults.TenantId && x.MarketId == market.Id && x.AsOfDate == asOfDate)
            .ToListAsync(ct);
        if (existingForDate.Count != 0)
            db.MenuIntelligenceRecords.RemoveRange(existingForDate);

        var inserted = 0;
        foreach (var row in ws.Rows(headerRowNumber.Value + 1, used.LastRowUsed().RowNumber()))
        {
            var rankStr = row.Cell(1).GetString().Trim();
            if (!int.TryParse(rankStr, out var rank))
                break;

            var productStr = row.Cell(2).GetString().Trim();
            if (string.IsNullOrWhiteSpace(productStr))
                break;

            var plannedOrders = row.Cell(3).GetValue<decimal>();
            var price = row.Cell(4).GetValue<decimal>();
            var unitCost = row.Cell(5).GetValue<decimal>();
            var grossProfit = row.Cell(6).GetValue<decimal>();
            var totalProfit = row.Cell(7).GetValue<decimal>();
            var menuAction = row.Cell(8).GetString().Trim();

            var (name, code) = ParseProduct(productStr);
            Product product;

            if (!string.IsNullOrWhiteSpace(code) && productByCode.TryGetValue(code, out var pByCode))
            {
                product = pByCode;
            }
            else if (productByName.TryGetValue(name, out var pByName))
            {
                product = pByName;
            }
            else
            {
                product = new Product
                {
                    TenantId = Defaults.TenantId,
                    OrgId = Defaults.OrgId,
                    Code = string.IsNullOrWhiteSpace(code) ? name : code,
                    Name = name,
                    UnitPrice = price,
                    UnitCost = unitCost
                };
                db.Products.Add(product);
                productByName[product.Name] = product;
                productByCode[product.Code] = product;
            }

            product.UnitCost = unitCost;
            product.UnitPrice = price;

            db.MenuIntelligenceRecords.Add(new MenuIntelligenceRecord
            {
                TenantId = Defaults.TenantId,
                OrgId = Defaults.OrgId,
                AsOfDate = asOfDate,
                Market = market,
                Rank = rank,
                Product = product,
                PlannedOrders = plannedOrders,
                Price = price,
                UnitCost = unitCost,
                GrossProfit = grossProfit,
                TotalProfit = totalProfit,
                MenuAction = menuAction
            });
            inserted++;
        }

        await EnsureDefaultTenantAsync(ct);
        await db.SaveChangesAsync(ct);
        return inserted;
    }

    private static (string name, string code) ParseProduct(string raw)
    {
        var m = ProductCodeRegex.Match(raw);
        if (!m.Success)
            return (raw.Trim(), "");

        var code = m.Groups["code"].Value.Trim();
        var name = ProductCodeRegex.Replace(raw, "").Trim();
        return (name, code);
    }

    private async Task EnsureDefaultTenantAsync(CancellationToken ct)
    {
        if (await db.Tenants.AnyAsync(x => x.Id == Defaults.TenantId, ct))
            return;

        db.Tenants.Add(new Tenant
        {
            Id = Defaults.TenantId,
            TenantId = Defaults.TenantId,
            OrgId = 0,
            Name = "Default"
        });
        await db.SaveChangesAsync(ct);
    }
}
