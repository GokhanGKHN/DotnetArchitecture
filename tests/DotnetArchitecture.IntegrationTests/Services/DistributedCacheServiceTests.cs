using System.Text.Json;
using DotnetArchitecture.Persistence.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Services;

public class DistributedCacheServiceTests
{
    private record ProductCacheItem(Guid Id, string Name, decimal Price);

    [Fact]
    public async Task SetAsync_And_GetAsync_ShouldStoreAndRetrieveObjectCorrectly()
    {
        // Arrange: Gerçek MemoryDistributedCache kullanarak serialize/deserialize döngüsünü doğrula
        var memoryDistributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(memoryDistributedCache, logger);

        var key = "product-123";
        var expectedItem = new ProductCacheItem(Guid.NewGuid(), "Gaming Mouse", 1250.50m);

        // Act
        await cacheService.SetAsync(key, expectedItem, TimeSpan.FromMinutes(10));
        var result = await cacheService.GetAsync<ProductCacheItem>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(expectedItem.Id);
        result.Name.Should().Be("Gaming Mouse");
        result.Price.Should().Be(1250.50m);
    }

    [Fact]
    public async Task RemoveAsync_ShouldEvictItemFromCache()
    {
        // Arrange
        var memoryDistributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(memoryDistributedCache, logger);

        var key = "product-to-delete";
        await cacheService.SetAsync(key, "Sample String", TimeSpan.FromMinutes(5));

        // Act
        await cacheService.RemoveAsync(key);
        var result = await cacheService.GetAsync<string>(key);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var memoryDistributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(memoryDistributedCache, logger);

        // Act
        var result = await cacheService.GetAsync<ProductCacheItem>("non-existent-key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WhenCacheThrowsException_ShouldCatchGracefullyAndReturnNull()
    {
        // Arrange: Redis sunucusu çöktüğünde veya ağ koptuğunda sistemin patlamadığını doğrula (Resilience)
        var failingCacheMock = Substitute.For<IDistributedCache>();
        failingCacheMock.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Redis connection timed out"));

        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(failingCacheMock, logger);

        // Act
        var result = await cacheService.GetAsync<ProductCacheItem>("any-key");

        // Assert
        result.Should().BeNull(); // Çökmek yerine cache miss gibi davranmalıdır!
    }

    [Fact]
    public async Task SetAsync_WhenCacheThrowsException_ShouldCatchGracefullyWithoutCrashing()
    {
        // Arrange
        var failingCacheMock = Substitute.For<IDistributedCache>();
        failingCacheMock.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Redis server unavailable"));

        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(failingCacheMock, logger);

        // Act
        var act = () => cacheService.SetAsync("any-key", "any-value", TimeSpan.FromMinutes(1));

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RemoveAsync_WhenCacheThrowsException_ShouldCatchGracefullyWithoutCrashing()
    {
        // Arrange
        var failingCacheMock = Substitute.For<IDistributedCache>();
        failingCacheMock.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Redis server unavailable"));

        var logger = Substitute.For<ILogger<DistributedCacheService>>();
        var cacheService = new DistributedCacheService(failingCacheMock, logger);

        // Act
        var act = () => cacheService.RemoveAsync("any-key");

        // Assert
        await act.Should().NotThrowAsync();
    }
}
