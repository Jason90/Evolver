using Evolver.Application.Security;
using Evolver.Core.Entities;
using Evolver.Shared.Api;
using Evolver.Shared.Dtos;
using Evolver.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    JwtTokenService tokenService,
    AppDbContext db,
    IOptions<JwtOptions> jwtOptions,
    IHostEnvironment hostEnvironment
) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        var trimmed = req.UserName?.Trim() ?? "";
        if (string.IsNullOrEmpty(trimmed))
            return BadRequest(ApiEnvelope.Fail("bad_request", "请输入用户名。"));

        var normalized = userManager.NormalizeName(trimmed);
        if (string.IsNullOrEmpty(normalized))
            return Unauthorized(ApiEnvelope.Fail("login_failed", "用户名或密码错误。"));

        if (req.TenantId is null)
        {
            var dup = await userManager.Users.CountAsync(u => u.NormalizedUserName == normalized);
            if (dup > 1)
            {
                return BadRequest(ApiEnvelope.Fail(
                    "tenant_required",
                    "存在同名用户，请在登录请求中填写租户编号（tenantId）。"));
            }
        }

        var user = await ResolveUserByLoginNameAsync(trimmed, req.TenantId);
        if (user is null)
            return Unauthorized(ApiEnvelope.Fail("login_failed", "用户名或密码错误。"));

        var tenantOk = await db.Tenants.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(t => t.Id == user.TenantId && t.IsActive && (t.ExpireAt == null || t.ExpireAt.Value.Date >= DateTime.UtcNow.Date));
        if (!tenantOk)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiEnvelope.Fail("tenant_disabled", "该租户已停用或已过期，暂不可登录。"));
        }

        if (!user.IsActive)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiEnvelope.Fail("account_disabled", "该账户已停用，请联系管理员为你重新开通。"));
        }

        var res = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!res.Succeeded)
            return Unauthorized(ApiEnvelope.Fail("login_failed", "用户名或密码错误。"));

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user, roles);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpMinutes);

        return Ok(new LoginResponseDto(token, "Bearer", expiresAt));
    }

    /// <summary>忘记密码：生成重置令牌。生产环境不在响应中返回令牌（需邮件等渠道）；开发环境可返回便于联调。</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ForgotPasswordResponseDto>> ForgotPassword([FromBody] ForgotPasswordRequestDto req)
    {
        var name = req.UserName?.Trim() ?? "";
        if (string.IsNullOrEmpty(name))
            return BadRequest("UserName is required.");

        if (req.TenantId is null)
        {
            var n = userManager.NormalizeName(name);
            if (!string.IsNullOrEmpty(n) && await userManager.Users.CountAsync(u => u.NormalizedUserName == n) > 1)
            {
                return BadRequest(ApiEnvelope.Fail(
                    "tenant_required",
                    "存在同名用户，请在请求中同时提供租户编号（tenantId）。"));
            }
        }

        var user = await ResolveUserByLoginNameAsync(name, req.TenantId);
        if (user is null || !user.IsActive)
        {
            return Ok(new ForgotPasswordResponseDto(
                null,
                "若该用户名存在且账户可用，可使用收到的重置链接或联系管理员。"));
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        if (hostEnvironment.IsDevelopment())
        {
            return Ok(new ForgotPasswordResponseDto(
                token,
                "开发环境：请在「重置密码」页使用下方返回的 Token 完成重置。"));
        }

        return Ok(new ForgotPasswordResponseDto(
            null,
            "若该用户名存在且账户可用，请使用邮件中的链接或联系管理员获取重置方式。"));
    }

    /// <summary>使用 Identity 重置令牌设置新密码。</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto req)
    {
        var name = req.UserName?.Trim() ?? "";
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(req.Token) || string.IsNullOrEmpty(req.NewPassword))
            return BadRequest("UserName, Token and NewPassword are required.");

        if (req.TenantId is null)
        {
            var n = userManager.NormalizeName(name);
            if (!string.IsNullOrEmpty(n) && await userManager.Users.CountAsync(u => u.NormalizedUserName == n) > 1)
            {
                return BadRequest(ApiEnvelope.Fail(
                    "tenant_required",
                    "存在同名用户，请在请求中同时提供租户编号（tenantId）。"));
            }
        }

        var user = await ResolveUserByLoginNameAsync(name, req.TenantId);
        if (user is null || !user.IsActive)
            return BadRequest("无效的用户名或令牌。");

        var res = await userManager.ResetPasswordAsync(user, req.Token, req.NewPassword);
        if (!res.Succeeded)
            return BadRequest(string.Join("; ", res.Errors.Select(e => e.Description)));

        return NoContent();
    }

    private async Task<AppUser?> ResolveUserByLoginNameAsync(string userName, int? tenantId)
    {
        var trimmed = userName.Trim();
        var normalized = userManager.NormalizeName(trimmed);
        if (string.IsNullOrEmpty(normalized))
            return null;

        var query = userManager.Users.Where(u => u.NormalizedUserName == normalized);
        if (tenantId is int tid)
            return await query.FirstOrDefaultAsync(u => u.TenantId == tid);

        var list = await query.ToListAsync();
        if (list.Count > 1)
            return null;
        return list.FirstOrDefault();
    }
}
