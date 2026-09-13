using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DotnetArchitecture.Application.Features.Auth;
using DotnetArchitecture.Application.Features.Auth.Commands.Register;
using DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;
using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using DotnetArchitecture.Domain.Enums;
using DotnetArchitecture.IntegrationTests.Common;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Controllers;

public class OrdersControllerIdempotencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrdersControllerIdempotencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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
    public async Task CreateOrder_WithDuplicateIdempotencyKey_ShouldReturnSameResponseAndDeductStockOnlyOnce()
    {
        // 1. Admin olarak oturum açıp 10 adet stoklu bir ürün oluşturalım
        var adminToken = await GetTokenAsync(UserRole.Admin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var productName = $"Idempotent Laptop {Guid.NewGuid().ToString()[..6]}";
        var createProductCmd = new CreateProductCommand(productName, 25000, 10);
        var productResponse = await _client.PostAsJsonAsync("/api/products", createProductCmd);
        productResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var productBody = await productResponse.Content.ReadAsStringAsync();
        using var productJson = JsonDocument.Parse(productBody);
        var productId = productJson.RootElement.GetProperty("id").GetGuid();

        // 2. Member olarak oturum açalım
        var memberToken = await GetTokenAsync(UserRole.Member);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

        var idempotencyKey = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var orderCommand = new CreateOrderCommand(
            customerId,
            new List<OrderItemRequest> { new OrderItemRequest(productId, 2) } // 2 adet alıyoruz
        );

        // 3. İLK İSTEK: Idempotency-Key başlığı ile sipariş gönderiyoruz
        var request1 = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(orderCommand)
        };
        request1.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        var response1 = await _client.SendAsync(request1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response1.Headers.Contains("Idempotency-Key").Should().BeTrue();

        var body1 = await response1.Content.ReadAsStringAsync();
        using var json1 = JsonDocument.Parse(body1);
        var orderId1 = json1.RootElement.GetProperty("id").GetGuid();

        // 4. İKİNCİ İSTEK (Mükerrer İstek / Network Retry): Aynı Idempotency-Key ile tekrar gönderiyoruz!
        var request2 = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(orderCommand)
        };
        request2.Headers.Add("Idempotency-Key", idempotencyKey.ToString());

        var response2 = await _client.SendAsync(request2);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body2 = await response2.Content.ReadAsStringAsync();
        using var json2 = JsonDocument.Parse(body2);
        var orderId2 = json2.RootElement.GetProperty("id").GetGuid();

        // ASSERTION 1: İki istek de TAMAMEN AYNI sipariş ID'sini döndürmelidir!
        orderId2.Should().Be(orderId1);

        // ASSERTION 2: Veritabanında stok kontrolü - Stok 10'dan 8'e düşmüş olmalı, ASLA 6'ya düşmemelidir!
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DotnetArchitecture.Persistence.Context.AppDbContext>();

        var productInDb = await dbContext.Products.FindAsync(productId);
        productInDb.Should().NotBeNull();
        productInDb!.StockQuantity.Should().Be(8); // Sadece 1 kez düşüldü!

        // ASSERTION 3: Veritabanında sipariş sayısı kontrolü - Sadece 1 adet sipariş kaydedilmiş olmalıdır!
        var ordersCount = dbContext.Orders.Count(o => o.CustomerId == customerId);
        ordersCount.Should().Be(1);
    }
}
