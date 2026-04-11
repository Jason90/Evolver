namespace Evolver.Shared.Dtos;

public sealed record MenuIntelligenceDto(
    DateOnly AsOfDate,
    string Market,
    int Rank,
    string ProductCode,
    string ProductName,
    decimal PlannedOrders,
    decimal Price,
    decimal UnitCost,
    decimal GrossProfit,
    decimal TotalProfit,
    string MenuAction
);

