namespace Evolver.Shared.Dtos;

// --- Organizations ---

public sealed record OrganizationTreeNodeDto(
    long Id,
    long? ParentId,
    string Name,
    string? OrgType,
    List<OrganizationTreeNodeDto> Children);

public sealed record OrganizationDto(long Id, long? ParentId, string Name, string? OrgType);

public sealed record CreateOrganizationDto(string Name, long? ParentId, string? OrgType);

public sealed record UpdateOrganizationDto(string Name, long? ParentId, string? OrgType);

// --- Users ---

public sealed record UserListItemDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    int OrgId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<long> OrganizationIds);

public sealed record CreateUserDto(
    string UserName,
    string Password,
    string? Email,
    string? PhoneNumber,
    bool? IsActive,
    IReadOnlyList<string>? RoleNames);

public sealed record UpdateUserDto(string? Email, string? PhoneNumber, bool? IsActive, int? OrgId);

public sealed record UserImportResultDto(int Created, int Updated, int Skipped, IReadOnlyList<string> Messages);

public sealed record SetUserRolesDto(IReadOnlyList<string> RoleNames);

public sealed record SetUserOrganizationsDto(IReadOnlyList<long> OrganizationIds);

public sealed record AdminChangePasswordDto(string NewPassword);

// --- Data dictionary ---

public sealed record DataDictionaryTypeDto(
    long Id,
    string TypeCode,
    string TypeName,
    string? Remark,
    bool IsActive,
    int SortOrder,
    DateTime? UpdateTime);

public sealed record UpsertDataDictionaryTypeDto(
    string TypeCode,
    string TypeName,
    string? Remark,
    bool IsActive,
    int SortOrder);

/// <summary>字典数据项。主键 <see cref="Id"/> 为字典编码；<see cref="ItemCode"/> 为类型内业务项编码。</summary>
public sealed record DataDictionaryItemDto(
    long Id,
    string CategoryCode,
    string ItemCode,
    string DisplayName,
    string? ItemValue,
    string? Remark,
    int SortOrder,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName);

public sealed record UpsertDataDictionaryItemDto(
    string CategoryCode,
    string ItemCode,
    string DisplayName,
    string? ItemValue,
    string? Remark,
    int SortOrder,
    bool IsActive);

