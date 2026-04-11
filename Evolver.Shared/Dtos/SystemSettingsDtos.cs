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

public sealed record UserListItemDto(long Id, string UserName, string? Email, string? PhoneNumber, IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    int OrgId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<long> OrganizationIds);

public sealed record CreateUserDto(string UserName, string Password, string? Email, string? PhoneNumber, IReadOnlyList<string>? RoleNames);

public sealed record UpdateUserDto(string? Email, string? PhoneNumber, int? OrgId);

public sealed record SetUserRolesDto(IReadOnlyList<string> RoleNames);

public sealed record SetUserOrganizationsDto(IReadOnlyList<long> OrganizationIds);

public sealed record AdminChangePasswordDto(string NewPassword);

// --- Data dictionary ---

public sealed record DataDictionaryItemDto(
    long Id,
    string CategoryCode,
    string ItemCode,
    string DisplayName,
    string? ItemValue,
    int SortOrder,
    bool IsActive);

public sealed record UpsertDataDictionaryItemDto(
    string CategoryCode,
    string ItemCode,
    string DisplayName,
    string? ItemValue,
    int SortOrder,
    bool IsActive);

// --- Roles (extra) ---

public sealed record UpdateRoleDto(string Name);
