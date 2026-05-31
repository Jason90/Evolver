namespace Evolver.Shared.Dtos;

public sealed record CustomerListItemDto(
    long Id,
    long? CustomerCategoryRefId,
    string? CustomerCategoryCode,
    string? CustomerCategoryName,
    string Name,
    string? Gender,
    DateOnly? Birthday,
    string? JobTitle,
    string? Phone,
    string? Email,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertCustomerDto(
    long? CustomerCategoryRefId,
    string Name,
    string? Gender,
    DateOnly? Birthday,
    string? JobTitle,
    string? Phone,
    string? Email,
    string? Remark,
    bool IsActive
);

public sealed record CustomerImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages
);
