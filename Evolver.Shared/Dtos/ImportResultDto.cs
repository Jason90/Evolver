namespace Evolver.Shared.Dtos;

public sealed record ImportResultDto(
    int ProductsUpserted,
    int MarketsUpserted,
    int SalesEntriesUpserted,
    int MenuIntelligenceRecordsUpserted,
    int InventorySnapshotsUpserted
);

