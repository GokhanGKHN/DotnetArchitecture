using DotnetArchitecture.Application.Common.Idempotency;
using DotnetArchitecture.Persistence.Services;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetArchitecture.IntegrationTests.Services;

public class IdempotencyServiceTests
{
    private readonly DistributedCacheService _cacheService;
    private readonly IdempotencyService _idempotencyService;

    public IdempotencyServiceTests()
    {
        var memoryDistributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cacheService = new DistributedCacheService(memoryDistributedCache, NullLogger<DistributedCacheService>.Instance);
        _idempotencyService = new IdempotencyService(_cacheService, NullLogger<IdempotencyService>.Instance);
    }

    [Fact]
    public async Task CheckAsync_WhenKeyIsNew_ShouldReturnFirstRun()
    {
        // Arrange
        var key = Guid.NewGuid();

        // Act
        var checkResult = await _idempotencyService.CheckAsync<string>(key);

        // Assert
        checkResult.Status.Should().Be(IdempotencyStatus.FirstRun);
        checkResult.StoredResponse.Should().BeNull();
    }

    [Fact]
    public async Task CheckAsync_WhenCalledConcurrentlyWhileInFlight_ShouldReturnInProgress()
    {
        // Arrange
        var key = Guid.NewGuid();

        // Act: 1. İstek kilidi alır
        var firstCheck = await _idempotencyService.CheckAsync<string>(key);

        // Act: 2. Paralel istek aynı anda gelir (İşlem henüz tamamlanmadı)
        var secondCheck = await _idempotencyService.CheckAsync<string>(key);

        // Assert
        firstCheck.Status.Should().Be(IdempotencyStatus.FirstRun);
        secondCheck.Status.Should().Be(IdempotencyStatus.InProgress);
    }

    [Fact]
    public async Task SaveResultAsync_ThenCheckAsync_ShouldReturnAlreadyProcessedWithStoredValue()
    {
        // Arrange
        var key = Guid.NewGuid();
        var expectedOrderId = Guid.NewGuid();

        // Act: İlk kontrol -> Kilit alındı
        var firstCheck = await _idempotencyService.CheckAsync<Guid>(key);
        firstCheck.Status.Should().Be(IdempotencyStatus.FirstRun);

        // Act: İşlem bitti ve sonuç kaydedildi
        await _idempotencyService.SaveResultAsync(key, expectedOrderId);

        // Act: Aynı anahtar tekrar sorgulandı (Replay)
        var replayCheck = await _idempotencyService.CheckAsync<Guid>(key);

        // Assert
        replayCheck.Status.Should().Be(IdempotencyStatus.AlreadyProcessed);
        replayCheck.StoredResponse.Should().Be(expectedOrderId);
    }

    [Fact]
    public async Task ReleaseAsync_ShouldClearInFlightLock_AllowingRetry()
    {
        // Arrange
        var key = Guid.NewGuid();

        // 1. Kilit al
        await _idempotencyService.CheckAsync<string>(key);

        // 2. Hata oldu, kilidi serbest bırak
        await _idempotencyService.ReleaseAsync(key);

        // 3. Tekrar dene
        var retryCheck = await _idempotencyService.CheckAsync<string>(key);

        // Assert: Tekrar FirstRun olmalıdır
        retryCheck.Status.Should().Be(IdempotencyStatus.FirstRun);
    }
}
