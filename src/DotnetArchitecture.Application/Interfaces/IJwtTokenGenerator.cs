using DotnetArchitecture.Domain.Entities;

namespace DotnetArchitecture.Application.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user);
}
