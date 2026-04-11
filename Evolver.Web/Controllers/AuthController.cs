using Evolver.Application.Security;
using Evolver.Core.Entities;
using Evolver.Shared.Dtos;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Evolver.Web.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    SignInManager<AppUser> signInManager,
    UserManager<AppUser> userManager,
    JwtTokenService tokenService,
    IOptions<JwtOptions> jwtOptions
) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto req)
    {
        var user = await userManager.FindByNameAsync(req.UserName);
        if (user is null || user.IsDeleted)
            return Unauthorized();

        var res = await signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!res.Succeeded)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);
        var token = tokenService.CreateToken(user, roles);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(jwtOptions.Value.ExpMinutes);

        return Ok(new LoginResponseDto(token, "Bearer", expiresAt));
    }
}
