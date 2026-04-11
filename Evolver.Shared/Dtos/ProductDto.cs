namespace Evolver.Shared.Dtos;

public sealed record ProductDto(
    long Id,
    string Code,
    string Name,
    decimal UnitPrice,
    decimal? UnitCost
);

public sealed record ProductCreateDto(
    string Code,
    string Name,
    decimal UnitPrice,
    decimal? UnitCost
);

public sealed record ProductUpdateDto(
    string Code,
    string Name,
    decimal UnitPrice,
    decimal? UnitCost
);

