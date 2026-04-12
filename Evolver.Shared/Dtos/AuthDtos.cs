namespace Evolver.Shared.Dtos;

public sealed record LoginRequestDto(string UserName, string Password, int? TenantId = null);

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt
);

