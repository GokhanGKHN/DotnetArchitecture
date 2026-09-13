using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Features.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new CreateProductCommand("Valid Name", 100, 5);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateProductCommand("", 100, 5);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WhenPriceIsNegative_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CreateProductCommand("Product", -10, 5);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Price");
    }
}
