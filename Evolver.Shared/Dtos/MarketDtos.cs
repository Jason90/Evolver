namespace Evolver.Shared.Dtos;

public sealed record MarketListItemDto(
    long Id,
    string Name,
    decimal RentAmount,
    string? Address,
    string? Phone,
    string? Website,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertMarketDto(
    string Name,
    decimal RentAmount,
    string? Address,
    string? Phone,
    string? Website,
    string? Remark,
    bool IsActive
);

public sealed record MarketImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages
);
