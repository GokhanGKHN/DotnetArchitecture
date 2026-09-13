using System.Net;
using System.Net.Http.Json;
using DotnetArchitecture.Application.Features.Auth.Commands.Login;
using DotnetArchitecture.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Middlewares;

public class RateLimitingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WhenExceeding10RequestsPerMinute_ShouldReturn429TooManyRequests()
    {
        // Arrange: Geçersiz bir login komutu (hızlı istek göndermek için)
        var invalidLogin = new LoginCommand("nonexistent@test.com", "Password123*");

        HttpResponseMessage lastResponse = null!;

        // Act: 10 izin verilen isteğin ardından 11. isteği gönderiyoruz
        for (int i = 0; i < 11; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/auth/login", invalidLogin);
        }

        // Assert: 11. istek Rate Limiter tarafından 429 ile engellenmelidir!
        lastResponse.Should().NotBeNull();
        lastResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        lastResponse.Headers.Should().ContainKey("Retry-After");

        var responseBody = await lastResponse.Content.ReadAsStringAsync();
        responseBody.Should().Contain("Too Many Requests");
        responseBody.Should().Contain("429");
    }
}
