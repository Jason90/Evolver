namespace Evolver.Shared.Dtos;

public sealed record BomLineDto(
    long Id,
    long ComponentProductId,
    string ComponentProductCode,
    string ComponentProductName,
    decimal Quantity,
    string Unit,
    int SortOrder,
    decimal? ScrapRate
);

public sealed record BomHeaderDto(
    long Id,
    long FinishedProductId,
    string FinishedProductCode,
    string FinishedProductName,
    int Version,
    bool IsActive,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyList<BomLineDto> Lines,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertBomLineDto(
    long? Id,
    long ComponentProductId,
    decimal Quantity,
    string Unit,
    int SortOrder,
    decimal? ScrapRate
);

public sealed record UpsertBomHeaderDto(
    long FinishedProductId,
    int Version,
    bool IsActive,
    DateOnly? EffectiveFrom,
    DateOnly? EffectiveTo,
    IReadOnlyList<UpsertBomLineDto> Lines
);

public sealed record ProductionOrderMaterialDto(
    long Id,
    long MaterialProductId,
    string MaterialProductCode,
    string MaterialProductName,
    decimal PlannedQuantity,
    decimal IssuedQuantity,
    decimal ReturnedQuantity
);

public sealed record ProductionWasteRecordDto(
    long Id,
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal WasteQuantity,
    string? Reason
);

public sealed record ProductionOrderDto(
    long Id,
    string OrderNo,
    long OutputProductId,
    string OutputProductCode,
    string OutputProductName,
    string Status,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    string? Notes,
    IReadOnlyList<ProductionOrderMaterialDto> Materials,
    IReadOnlyList<ProductionWasteRecordDto> Wastes,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertProductionOrderMaterialDto(
    long? Id,
    long MaterialProductId,
    decimal PlannedQuantity,
    decimal IssuedQuantity,
    decimal ReturnedQuantity
);

public sealed record UpsertProductionWasteRecordDto(
    long? Id,
    long ProductId,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    decimal WasteQuantity,
    string? Reason
);

public sealed record UpsertProductionOrderDto(
    string OrderNo,
    long OutputProductId,
    string Status,
    decimal PlannedQuantity,
    decimal ActualQuantity,
    string? Notes,
    IReadOnlyList<UpsertProductionOrderMaterialDto> Materials,
    IReadOnlyList<UpsertProductionWasteRecordDto> Wastes
);

public sealed record InventoryTransactionDto(
    long Id,
    string TransactionType,
    long ProductId,
    string ProductCode,
    string ProductName,
    decimal Quantity,
    decimal BeforeQuantity,
    decimal AfterQuantity,
    string SourceType,
    long? SourceId,
    string? ReferenceNo,
    DateTime OccurredAt,
    string? Notes,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record CreateInventoryTransactionDto(
    string TransactionType,
    long ProductId,
    decimal Quantity,
    decimal BeforeQuantity,
    decimal AfterQuantity,
    string SourceType,
    long? SourceId,
    string? ReferenceNo,
    DateTime? OccurredAt,
    string? Notes
);
