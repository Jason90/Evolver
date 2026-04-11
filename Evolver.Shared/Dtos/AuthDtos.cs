namespace Evolver.Shared.Dtos;

public sealed record LoginRequestDto(string UserName, string Password);

public sealed record LoginResponseDto(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt
);

