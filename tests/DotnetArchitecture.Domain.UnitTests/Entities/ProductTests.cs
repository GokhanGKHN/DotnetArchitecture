using DotnetArchitecture.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.Domain.UnitTests.Entities;

public class ProductTests
{
    [Fact]
    public void Constructor_WithValidArguments_ShouldCreateProduct()
    {
        // Act
        var product = new Product("Laptop", 25000, 10);

        // Assert
        product.Name.Should().Be("Laptop");
        product.Price.Should().Be(25000);
        product.StockQuantity.Should().Be(10);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act
        var act = () => new Product(invalidName!, 100, 5);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Ürün adı boş olamaz.*");
    }

    [Fact]
    public void DeductStock_WhenSufficientStock_ShouldDecreaseStockQuantity()
    {
        // Arrange
        var product = new Product("Mouse", 500, 20);

        // Act
        product.DeductStock(5);

        // Assert
        product.StockQuantity.Should().Be(15);
    }

    [Fact]
    public void DeductStock_WhenInsufficientStock_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var product = new Product("Monitor", 4000, 3);

        // Act
        var act = () => product.DeductStock(5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Yetersiz stok!*");
    }
}
