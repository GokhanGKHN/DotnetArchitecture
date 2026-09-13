using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.Domain.UnitTests.Entities;

public class UserTests
{
    [Fact]
    public void Create_ShouldNormalizeEmailAndSetProperties()
    {
        // Act
        var user = User.Create(
            email: "  TestUser@Example.COM  ",
            firstName: "  Ali  ",
            lastName: "  Veli  ",
            passwordHash: "hashed_secret",
            role: UserRole.Admin);

        // Assert
        user.Email.Should().Be("testuser@example.com");
        user.FirstName.Should().Be("Ali");
        user.LastName.Should().Be("Veli");
        user.FullName.Should().Be("Ali Veli");
        user.PasswordHash.Should().Be("hashed_secret");
        user.Role.Should().Be(UserRole.Admin);
    }
}
