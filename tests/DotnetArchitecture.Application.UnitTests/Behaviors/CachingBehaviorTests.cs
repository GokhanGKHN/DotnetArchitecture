using DotnetArchitecture.Application.Behaviors;
using DotnetArchitecture.Application.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Behaviors;

public class CachingBehaviorTests
{
    private readonly ICacheService _cacheService;

    public CachingBehaviorTests()
    {
        _cacheService = Substitute.For<ICacheService>();
    }

    public class TestCacheableQuery : ICacheableQuery
    {
        public string CacheKey => "test-query-key";
        public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
    }

    public class TestInvalidatorCommand : ICacheInvalidator
    {
        public IReadOnlyCollection<string> CacheKeysToInvalidate => new[] { "key-1", "key-2" };
    }

    public class PlainRequest
    {
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDataWithoutCallingNext()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestCacheableQuery, string>>.Instance;
        var behavior = new CachingBehavior<TestCacheableQuery, string>(_cacheService, logger);
        var query = new TestCacheableQuery();
        var nextMock = Substitute.For<RequestHandlerDelegate<string>>();

        _cacheService.GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>("Cached Value"));

        // Act
        var result = await behavior.Handle(query, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be("Cached Value");
        await nextMock.DidNotReceive().Invoke(Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCallNextAndSaveToCache()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestCacheableQuery, string>>.Instance;
        var behavior = new CachingBehavior<TestCacheableQuery, string>(_cacheService, logger);
        var query = new TestCacheableQuery();
        var nextMock = Substitute.For<RequestHandlerDelegate<string>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Fresh Value From DB"));

        _cacheService.GetAsync<string>(query.CacheKey, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<string?>(null));

        // Act
        var result = await behavior.Handle(query, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be("Fresh Value From DB");
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
        await _cacheService.Received(1).SetAsync(query.CacheKey, "Fresh Value From DB", query.Expiration, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCacheInvalidator_ShouldCallNextAndInvalidateKeys()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<TestInvalidatorCommand, string>>.Instance;
        var behavior = new CachingBehavior<TestInvalidatorCommand, string>(_cacheService, logger);
        var command = new TestInvalidatorCommand();
        var nextMock = Substitute.For<RequestHandlerDelegate<string>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Success"));

        // Act
        var result = await behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be("Success");
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync("key-1", Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveAsync("key-2", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRequestIsNotCacheable_ShouldCallNextDirectlyWithoutTouchingCache()
    {
        // Arrange
        var logger = NullLogger<CachingBehavior<PlainRequest, string>>.Instance;
        var behavior = new CachingBehavior<PlainRequest, string>(_cacheService, logger);
        var request = new PlainRequest();
        var nextMock = Substitute.For<RequestHandlerDelegate<string>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult("Direct Result"));

        // Act
        var result = await behavior.Handle(request, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be("Direct Result");
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
        await _cacheService.DidNotReceive().GetAsync<string>(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
