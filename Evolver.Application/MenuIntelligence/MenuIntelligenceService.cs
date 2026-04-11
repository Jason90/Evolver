using Evolver.Infrastructure.Persistence;
using Evolver.Shared;
using Evolver.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Evolver.Application.MenuIntelligence;

public sealed class MenuIntelligenceService(AppDbContext db)
{
    public async Task<IReadOnlyList<MenuIntelligenceDto>> GetLatestAsync(string? market, CancellationToken ct = default)
    {
        var q = db.MenuIntelligenceRecords
            .AsNoTracking()
            .Include(x => x.Market)
            .Include(x => x.Product)
            .Where(x => x.TenantId == Defaults.TenantId);

        if (!string.IsNullOrWhiteSpace(market))
            q = q.Where(x => x.Market.Name == market);

        var latestDate = await q.Select(x => x.AsOfDate).OrderByDescending(x => x).FirstOrDefaultAsync(ct);
        if (latestDate == default)
            return [];

        var rows = await q.Where(x => x.AsOfDate == latestDate)
            .OrderBy(x => x.Rank)
            .ToListAsync(ct);

        return rows.Select(x => new MenuIntelligenceDto(
            AsOfDate: x.AsOfDate,
            Market: x.Market.Name,
            Rank: x.Rank,
            ProductCode: x.Product.Code,
            ProductName: x.Product.Name,
            PlannedOrders: x.PlannedOrders,
            Price: x.Price,
            UnitCost: x.UnitCost,
            GrossProfit: x.GrossProfit,
            TotalProfit: x.TotalProfit,
            MenuAction: x.MenuAction
        )).ToList();
    }
}
