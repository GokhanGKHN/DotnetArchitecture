using DotnetArchitecture.Application.Behaviors;
using DotnetArchitecture.Application.Common.Exceptions;
using DotnetArchitecture.Application.Common.Idempotency;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Behaviors;

public class IdempotencyBehaviorTests
{
    private readonly IIdempotencyService _idempotencyService;

    public IdempotencyBehaviorTests()
    {
        _idempotencyService = Substitute.For<IIdempotencyService>();
    }

    public record TestCommand(string Name, Guid? IdempotencyKey) : IIdempotentCommand<Guid>;
    public record PlainCommand(string Name) : IRequest<Guid>;

    [Fact]
    public async Task Handle_WhenFirstRun_ShouldExecuteHandlerAndSaveResult()
    {
        // Arrange
        var key = Guid.NewGuid();
        var command = new TestCommand("Place Order", key);
        var expectedResult = Guid.NewGuid();

        _idempotencyService.CheckAsync<Guid>(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IdempotencyCheckResult<Guid>(IdempotencyStatus.FirstRun)));

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(expectedResult));

        var behavior = new IdempotencyBehavior<TestCommand, Guid>(_idempotencyService, NullLogger<IdempotencyBehavior<TestCommand, Guid>>.Instance);

        // Act
        var result = await behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
        await _idempotencyService.Received(1).SaveResultAsync(key, expectedResult, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyProcessed_ShouldReturnStoredResultWithoutCallingHandler()
    {
        // Arrange
        var key = Guid.NewGuid();
        var command = new TestCommand("Place Order", key);
        var previouslySavedResult = Guid.NewGuid();

        _idempotencyService.CheckAsync<Guid>(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IdempotencyCheckResult<Guid>(IdempotencyStatus.AlreadyProcessed, previouslySavedResult)));

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        var behavior = new IdempotencyBehavior<TestCommand, Guid>(_idempotencyService, NullLogger<IdempotencyBehavior<TestCommand, Guid>>.Instance);

        // Act
        var result = await behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert: Daha önce işlendiği için kayıtlı yanıt dönmeli ve handler ASLA çağrılmamalıdır
        result.Should().Be(previouslySavedResult);
        await nextMock.DidNotReceive().Invoke(Arg.Any<CancellationToken>());
        await _idempotencyService.DidNotReceive().SaveResultAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenInProgress_ShouldThrowIdempotencyConflictExceptionWithoutCallingHandler()
    {
        // Arrange
        var key = Guid.NewGuid();
        var command = new TestCommand("Place Order", key);

        _idempotencyService.CheckAsync<Guid>(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IdempotencyCheckResult<Guid>(IdempotencyStatus.InProgress)));

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        var behavior = new IdempotencyBehavior<TestCommand, Guid>(_idempotencyService, NullLogger<IdempotencyBehavior<TestCommand, Guid>>.Instance);

        // Act
        var act = () => behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<IdempotencyConflictException>();
        exception.Which.IdempotencyKey.Should().Be(key);
        await nextMock.DidNotReceive().Invoke(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldReleaseLockAndRethrow()
    {
        // Arrange
        var key = Guid.NewGuid();
        var command = new TestCommand("Place Order", key);

        _idempotencyService.CheckAsync<Guid>(key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new IdempotencyCheckResult<Guid>(IdempotencyStatus.FirstRun)));

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("Yetersiz stok"));

        var behavior = new IdempotencyBehavior<TestCommand, Guid>(_idempotencyService, NullLogger<IdempotencyBehavior<TestCommand, Guid>>.Instance);

        // Act
        var act = () => behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert: Hata fırlatıldığında kilit serbest bırakılmalıdır (ReleaseAsync)
        await act.Should().ThrowAsync<InvalidOperationException>();
        await _idempotencyService.Received(1).ReleaseAsync(key, Arg.Any<CancellationToken>());
        await _idempotencyService.DidNotReceive().SaveResultAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCommandHasNoIdempotencyKey_ShouldCallNextDirectly()
    {
        // Arrange
        var command = new TestCommand("No Key", null);
        var expectedResult = Guid.NewGuid();

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(expectedResult));

        var behavior = new IdempotencyBehavior<TestCommand, Guid>(_idempotencyService, NullLogger<IdempotencyBehavior<TestCommand, Guid>>.Instance);

        // Act
        var result = await behavior.Handle(command, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
        await _idempotencyService.DidNotReceive().CheckAsync<Guid>(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
