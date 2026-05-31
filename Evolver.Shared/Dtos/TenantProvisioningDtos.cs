namespace Evolver.Shared.Dtos;

/// <summary>由平台租户管理员开通新租户。</summary>
public sealed record ProvisionTenantRequestDto(
    string TenantName,
    string AdminUserName,
    string AdminPassword,
    string? RootOrgName = null,
    string? AdminEmail = null,
    bool IsActive = true,
    DateTime? ExpireAt = null,
    string? Remark = null);

public sealed record ProvisionTenantResponseDto(
    int TenantId,
    string TenantName,
    string RootOrgName,
    string AdminUserName);

/// <summary>租户列表行：与开通表单字段一致（不含密码）。根组织、管理员信息来自库内根组织节点与 Admin 角色用户（取 Id 最小者）。</summary>
public sealed record TenantListItemDto(
    int TenantId,
    string TenantName,
    string RootOrgName,
    DateTime? CreateTime,
    DateTime? UpdateTime,
    DateTime? ExpireAt,
    bool IsActive,
    string StatusText,
    string AdminUserName,
    string? AdminEmail,
    string? Remark);

public sealed record UpdateTenantDto(
    string TenantName,
    string RootOrgName,
    DateTime? ExpireAt,
    bool IsActive,
    string? AdminEmail,
    string? Remark);

/// <summary>批量开通租户导入结果（仅新建，无「更新」概念）。</summary>
public sealed record TenantImportResultDto(int Created, int Skipped, IReadOnlyList<string> Messages);
