namespace Evolver.Shared.Dtos;

// --- Organizations ---

public sealed record OrganizationTreeNodeDto(
    long Id,
    long? ParentId,
    string Name,
    string? OrgType,
    bool IsActive,
    DateTime? UpdateTime,
    List<OrganizationTreeNodeDto> Children);

public sealed record OrganizationDto(
    long Id,
    long? ParentId,
    string Name,
    string? OrgType,
    bool IsActive,
    DateTime? UpdateTime);

public sealed record CreateOrganizationDto(string Name, long? ParentId, string? OrgType, bool IsActive = true);

public sealed record UpdateOrganizationDto(string Name, long? ParentId, string? OrgType, bool IsActive);

// --- Users ---

public sealed record UserListItemDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    long? DepartmentId,
    string? DepartmentName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName,
    string? Remark);

public sealed record UserDetailDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    int OrgId,
    long? DepartmentId,
    string? Remark,
    IReadOnlyList<string> Roles,
    IReadOnlyList<long> OrganizationIds);

public sealed record CreateUserDto(
    string UserName,
    string Password,
    string? Email,
    string? PhoneNumber,
    string? Remark,
    long? DepartmentId,
    bool? IsActive,
    IReadOnlyList<string>? RoleNames);

public sealed record UpdateUserDto(string? Email, string? PhoneNumber, string? Remark, long? DepartmentId, bool? IsActive, int? OrgId);

public sealed record UserImportResultDto(int Created, int Updated, int Skipped, IReadOnlyList<string> Messages);

public sealed record SetUserRolesDto(IReadOnlyList<string> RoleNames);

public sealed record SetUserOrganizationsDto(IReadOnlyList<long> OrganizationIds);

public sealed record AdminChangePasswordDto(string NewPassword);
public sealed record MoveUserDepartmentDto(long DepartmentId);

// --- Units ---

public sealed record UnitListItemDto(
    long Id,
    string Code,
    string Name,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName);

public sealed record UpsertUnitDto(
    string Code,
    string Name,
    bool IsActive);

public sealed record UnitImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages);

// --- System parameters ---

public sealed record SystemParameterListItemDto(
    long Id,
    string Name,
    string ParamKey,
    string ParamValue,
    bool IsSystemBuiltIn,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName
);

public sealed record UpsertSystemParameterDto(
    string Name,
    string ParamKey,
    string ParamValue,
    bool IsSystemBuiltIn,
    string? Remark,
    bool IsActive
);

public sealed record SystemParameterImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages);

// --- Suppliers ---

public sealed record SupplierListItemDto(
    long Id,
    string Name,
    string? Address,
    string? Phone,
    string? Website,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName);

public sealed record UpsertSupplierDto(
    string Name,
    string? Address,
    string? Phone,
    string? Website,
    string? Remark,
    bool IsActive);

public sealed record SupplierImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages);

// --- Customer categories ---

public sealed record CustomerCategoryListItemDto(
    long Id,
    string CategoryCode,
    string Name,
    string? Remark,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName);

public sealed record UpsertCustomerCategoryDto(
    string CategoryCode,
    string Name,
    string? Remark,
    bool IsActive);

public sealed record CustomerCategoryImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages);

// --- Product categories ---

public sealed record ProductCategoryTreeNodeDto(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName,
    List<ProductCategoryTreeNodeDto> Children);

public sealed record ProductCategoryDto(
    long Id,
    long? ParentId,
    string Code,
    string Name,
    bool IsActive,
    DateTime? UpdateTime,
    int? UpdateBy,
    string? UpdateByUserName);

public sealed record CreateProductCategoryDto(
    string Code,
    string Name,
    long? ParentId,
    bool IsActive);

public sealed record UpdateProductCategoryDto(
    string Code,
    string Name,
    long? ParentId,
    bool IsActive);

public sealed record ProductCategoryImportResultDto(
    int Created,
    int Updated,
    int Skipped,
    IReadOnlyList<string> Messages);

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

// --- Enum config ---

public sealed record EnumTypeDto(
    string EnumTypeCode,
    string Name,
    string? Description,
    bool IsActive,
    DateTime? UpdateTime);

public sealed record UpsertEnumTypeDto(
    string EnumTypeCode,
    string Name,
    string? Description,
    bool IsActive);

public sealed record EnumValueDto(
    string EnumTypeCode,
    string EnumValueCode,
    string Name,
    int SortNo,
    bool IsDefault,
    string? Description,
    bool IsActive,
    DateTime? UpdateTime);

public sealed record UpsertEnumValueDto(
    string EnumTypeCode,
    string EnumValueCode,
    string Name,
    int SortNo,
    bool IsDefault,
    string? Description,
    bool IsActive);

