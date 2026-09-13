namespace DotnetArchitecture.Application.Features.Auth;

public record AuthResponse(
    string Token,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime ExpiresAtUtc
);
