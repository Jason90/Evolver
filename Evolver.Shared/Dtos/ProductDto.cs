namespace Evolver.Shared.Dtos;

public sealed record ProductListItemDto(
    long Id,
    string Code,
    string Name,
    string? ProductTypeCode,
    string? UnitCode,
    string? Barcode,
    string? Brand,
    string? Model,
    decimal? UnitCost,
    decimal? SuggestedPrice,
    decimal TheoreticalStock,
    decimal ActualStock,
    decimal AlertStock,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertProductDto(
    string Code,
    string Name,
    string? ProductTypeCode,
    string? UnitCode,
    string? Barcode,
    string? Brand,
    string? Model,
    decimal? UnitCost,
    decimal? SuggestedPrice,
    decimal TheoreticalStock,
    decimal ActualStock,
    decimal AlertStock,
    string? Remark,
    bool IsActive
);

public sealed record ProductImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages
);

