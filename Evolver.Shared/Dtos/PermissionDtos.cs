namespace Evolver.Shared.Dtos;

public sealed record PermissionDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Name,
    string? Resource
);

public sealed record PermissionTreeNodeDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Name,
    string? Resource,
    List<PermissionTreeNodeDto> Children
);

public sealed record UpsertPermissionDto(
    long? ParentId,
    string Type,
    string Code,
    string Name,
    string? Resource
);

