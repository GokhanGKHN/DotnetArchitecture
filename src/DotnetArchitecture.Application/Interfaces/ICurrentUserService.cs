namespace DotnetArchitecture.Application.Interfaces;

/// <summary>
/// O anki HTTP isteğini gerçekleştiren kullanıcının kimlik bilgilerine erişim sağlayan servis arayüzü.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    bool IsAuthenticated { get; }
}
