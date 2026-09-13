using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DotnetArchitecture.Application.Common;
using DotnetArchitecture.Application.Features.Auth;
using DotnetArchitecture.Application.Features.Auth.Commands.Register;
using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;
using DotnetArchitecture.Domain.Enums;
using DotnetArchitecture.IntegrationTests.Common;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Controllers;

public class ProductsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> GetTokenAsync(UserRole role)
    {
        var email = $"user_{Guid.NewGuid()}@test.com";
        var register = new RegisterCommand(email, "Password123*", "Test", "User", role);
        var response = await _client.PostAsJsonAsync("/api/auth/register", register);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return auth!.Token;
    }

    [Fact]
    public async Task Create_WithoutToken_ShouldReturn401Unauthorized()
    {
        // Arrange
        var command = new CreateProductCommand("Secret Product", 100, 5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithMemberToken_ShouldReturn403Forbidden()
    {
        // Arrange
        var memberToken = await GetTokenAsync(UserRole.Member);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var command = new CreateProductCommand("Forbidden Product", 100, 5);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithAdminToken_ShouldReturn200Ok()
    {
        // Arrange
        var adminToken = await GetTokenAsync(UserRole.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var command = new CreateProductCommand("Admin Product", 2500, 10);

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_WithoutToken_ShouldReturn200OkWithPagedResponse()
    {
        // Act (Herkese açık endpoint)
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/products?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>();
        pagedResponse.Should().NotBeNull();
        pagedResponse!.PageNumber.Should().Be(1);
        pagedResponse.PageSize.Should().Be(5);
    }
}
