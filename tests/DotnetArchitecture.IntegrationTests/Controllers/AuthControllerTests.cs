using System.Net;
using System.Net.Http.Json;
using DotnetArchitecture.Application.Features.Auth;
using DotnetArchitecture.Application.Features.Auth.Commands.Login;
using DotnetArchitecture.Application.Features.Auth.Commands.Register;
using DotnetArchitecture.Domain.Enums;
using DotnetArchitecture.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn200AndJwtToken()
    {
        // Arrange
        var command = new RegisterCommand("newuser1@test.com", "Password123*", "Test", "User", UserRole.Member);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", command);

        // Assert
        var errorContent = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, because: errorContent);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrWhiteSpace();
        authResponse.Email.Should().Be("newuser1@test.com");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200AndToken()
    {
        // Arrange - önce kullanıcı kaydet
        var email = $"logintest_{Guid.NewGuid()}@test.com";
        var registerCommand = new RegisterCommand(email, "Password123*", "Login", "User", UserRole.Member);
        await _client.PostAsJsonAsync("/api/auth/register", registerCommand);

        var loginCommand = new LoginCommand(email, "Password123*");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        authResponse.Should().NotBeNull();
        authResponse!.Token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn400BadRequest()
    {
        // Arrange
        var loginCommand = new LoginCommand("nonexistent@test.com", "WrongPassword");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
