using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.IntegrationTests.Common;
using DotnetArchitecture.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Interceptors;

public class AuditableEntityInterceptorTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceProvider _serviceProvider;

    public AuditableEntityInterceptorTests(CustomWebApplicationFactory factory)
    {
        _serviceProvider = factory.Services;
    }

    [Fact]
    public async Task SaveChanges_WhenAddingEntity_ShouldSetCreatedAtUtcAndCreatedBy()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product("Audit Test Product", 150m, 10);

        // Act
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert
        product.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.CreatedBy.Should().NotBeNullOrWhiteSpace();
        product.LastModifiedAtUtc.Should().BeNull();
        product.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task SaveChanges_WhenModifyingEntity_ShouldSetLastModifiedAtUtcAndLastModifiedBy()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product("Update Test Product", 200m, 5);
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        product.UpdatePrice(250m);
        await context.SaveChangesAsync();

        // Assert
        product.LastModifiedAtUtc.Should().NotBeNull();
        product.LastModifiedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.LastModifiedBy.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveChanges_WhenDeletingEntity_ShouldPerformSoftDeleteAndApplyGlobalQueryFilter()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var product = new Product("Soft Delete Product", 99m, 3);
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act: Varlığı siliyoruz (Remove)
        context.Products.Remove(product);
        await context.SaveChangesAsync();

        // Assert 1: Interceptor fiziksel silmeyi engelleyip IsDeleted bayrağını set etmeli
        product.IsDeleted.Should().BeTrue();
        product.DeletedAtUtc.Should().NotBeNull();
        product.DeletedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        product.DeletedBy.Should().NotBeNullOrWhiteSpace();

        // Assert 2: Global Query Filter sayesinde normal sorgularda bu ürün gelmemeli!
        var queryResult = await context.Products.FirstOrDefaultAsync(p => p.Id == product.Id);
        queryResult.Should().BeNull();

        // Assert 3: IgnoreQueryFilters() ile silinmiş kayıtlar geçmişe yönelik görüntülenebilmeli!
        var rawResult = await context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == product.Id);
        rawResult.Should().NotBeNull();
        rawResult!.IsDeleted.Should().BeTrue();
    }
}
