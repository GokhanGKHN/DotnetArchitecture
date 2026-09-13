using DotnetArchitecture.Application.Behaviors;
using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationExceptionWithoutCallingNext()
    {
        // Arrange
        var validators = new List<IValidator<CreateProductCommand>>
        {
            new CreateProductCommandValidator()
        };

        var behavior = new ValidationBehavior<CreateProductCommand, Guid>(validators);

        var invalidCommand = new CreateProductCommand("", -50, -5); // Hatalı komut
        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();

        // Act
        var act = () => behavior.Handle(invalidCommand, nextMock, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();

        // next delegate'i ASLA çağrılmamalıdır (Turnikeden geçemedi)
        await nextMock.DidNotReceive().Invoke(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldCallNextDelegate()
    {
        // Arrange
        var validators = new List<IValidator<CreateProductCommand>>
        {
            new CreateProductCommandValidator()
        };

        var behavior = new ValidationBehavior<CreateProductCommand, Guid>(validators);

        var validCommand = new CreateProductCommand("Valid Monitor", 5000, 10);
        var expectedGuid = Guid.NewGuid();

        var nextMock = Substitute.For<RequestHandlerDelegate<Guid>>();
        nextMock.Invoke(Arg.Any<CancellationToken>()).Returns(Task.FromResult(expectedGuid));

        // Act
        var result = await behavior.Handle(validCommand, nextMock, CancellationToken.None);

        // Assert
        result.Should().Be(expectedGuid);
        await nextMock.Received(1).Invoke(Arg.Any<CancellationToken>());
    }
}
