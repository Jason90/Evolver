namespace Evolver.Shared.Dtos;

public sealed record RoleDto(long Id, string Name);

public sealed record CreateRoleDto(string Name);

public sealed record SetRolePermissionsDto(List<long> PermissionIds);

