namespace Evolver.Shared.Dtos;

public sealed record SalesEntryDto(
    long Id,
    DateOnly Date,
    string Market,
    string ProductCode,
    string ProductName,
    decimal UnitsSold,
    decimal UnitPrice,
    decimal SalesValue,
    string? Notes
);

