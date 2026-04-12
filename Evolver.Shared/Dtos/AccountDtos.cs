namespace Evolver.Shared.Dtos;

/// <summary>当前登录用户资料（与 Identity 用户表一致的核心字段）。</summary>
public sealed record AccountProfileDto(
    long Id,
    string UserName,
    string? Email,
    string? PhoneNumber,
    int TenantId,
    int OrgId,
    bool IsActive,
    IReadOnlyList<string> Roles,
    bool CanProvisionTenants);

public sealed record ChangeOwnPasswordDto(string CurrentPassword, string NewPassword);

public sealed record ForgotPasswordRequestDto(string UserName, int? TenantId = null);

/// <summary>忘记密码响应：生产环境不返回 Token；开发环境可在 <see cref="ResetToken"/> 中带一次性令牌。</summary>
public sealed record ForgotPasswordResponseDto(string? ResetToken, string Message);

public sealed record ResetPasswordRequestDto(string UserName, string Token, string NewPassword, int? TenantId = null);
