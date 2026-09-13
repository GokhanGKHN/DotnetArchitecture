using DotnetArchitecture.Domain.Common;
using DotnetArchitecture.Domain.Enums;

namespace DotnetArchitecture.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Member;

    public string FullName => $"{FirstName} {LastName}".Trim();

    private User() { } // EF Core için boş constructor

    public static User Create(
        string email,
        string firstName,
        string lastName,
        string passwordHash,
        UserRole role = UserRole.Member)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
