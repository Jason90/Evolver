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

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, "jwt_user"),
            new Claim(ClaimTypes.AuthenticationMethod, "Bearer")
        }, authenticationType: "Bearer");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }
}
