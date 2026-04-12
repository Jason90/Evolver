namespace Evolver.Shared.Dtos;

public sealed record PermissionDto(
    long Id,
    long? ParentId,
    string Type,
    string Code,
    string Name,
    string? Resource,
    string? ComponentPath,
    int SortOrder,
    bool IsEnabled,
    string? Icon,
    bool IsExternalLink,
    bool IsVisible
);

public sealed record PermissionTreeNodeDto(
    long Id,
    long? ParentId,
    string Type,
    string DisplayType,
    string Code,
    string Name,
    string? Resource,
    string? ComponentPath,
    int SortOrder,
    bool IsEnabled,
    string? Icon,
    bool IsExternalLink,
    bool IsVisible,
    List<PermissionTreeNodeDto> Children
);

public sealed record UpsertPermissionDto(
    long? ParentId,
    string Type,
    string Code,
    string Name,
    string? Resource,
    string? ComponentPath = null,
    int? SortOrder = null,
    bool? IsEnabled = null,
    string? Icon = null,
    bool? IsExternalLink = null,
    bool? IsVisible = null
);

