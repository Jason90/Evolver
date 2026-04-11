namespace Evolver.Application.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Evolver";
    public string Audience { get; init; } = "Evolver";
    public string SigningKey { get; init; } = "dev-only-change-me";
    public int ExpMinutes { get; init; } = 720;
}
