namespace Evolver.Shared.Dtos;

public sealed record RoleDto(
    long Id,
    string Name,
    int TenantId = 0,
    int OrgId = 0,
    string? OrgName = null,
    string? NormalizedName = null,
    int? UpdateBy = null,
    DateTime? UpdateTime = null);

public sealed record RoleDetailDto(long Id, string Name, int TenantId, int OrgId);

public sealed record CreateRoleDto(string Name, int? OrgId = null);

public sealed record UpdateRoleDto(string Name, int? OrgId = null);

public sealed record SetRolePermissionsDto(List<long> PermissionIds);

public sealed record RoleImportResultDto(int Created, int Updated, int Skipped, IReadOnlyList<string> Messages);
