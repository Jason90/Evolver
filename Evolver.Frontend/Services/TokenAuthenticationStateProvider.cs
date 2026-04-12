using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Evolver.Frontend.Services;

public sealed class TokenAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private readonly AuthTokenStore _tokens;

    public TokenAuthenticationStateProvider(AuthTokenStore tokens)
    {
        _tokens = tokens;
        _tokens.AuthenticationChanged += OnTokenChanged;
    }

    private void OnTokenChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (string.IsNullOrWhiteSpace(_tokens.AccessToken))
            return Task.FromResult(new AuthenticationState(Anonymous));

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(_tokens.AccessToken);
            var claims = new List<Claim>();
            foreach (var c in jwt.Claims)
                claims.Add(c);

            if (!claims.Any(c => c.Type == ClaimTypes.Name))
            {
                var name = jwt.Claims.FirstOrDefault(c => c.Type is "name" or "unique_name" or ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(name))
                    claims.Add(new Claim(ClaimTypes.Name, name));
            }

            if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            {
                var sub = jwt.Subject;
                if (!string.IsNullOrEmpty(sub))
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            var identity = new ClaimsIdentity(claims, authenticationType: "Bearer", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
        catch
        {
            return Task.FromResult(new AuthenticationState(Anonymous));
        }
    }
}
