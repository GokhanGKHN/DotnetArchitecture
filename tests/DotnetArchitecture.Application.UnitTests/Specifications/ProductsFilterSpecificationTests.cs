using DotnetArchitecture.Application.Features.Products.Specifications;
using DotnetArchitecture.Domain.Entities;
using FluentAssertions;

namespace DotnetArchitecture.Application.UnitTests.Specifications;

public class ProductsFilterSpecificationTests
{
    [Fact]
    public void Constructor_WithSearchTerm_ShouldFilterMatchingProducts()
    {
        // Arrange
        var searchTerm = "Laptop";
        var spec = new ProductsFilterSpecification(pageNumber: 1, pageSize: 10, searchTerm: searchTerm);

        var matchingProduct = new Product("Gaming Laptop", 25000m, 5);
        var nonMatchingProduct = new Product("Wireless Mouse", 500m, 20);

        // Act
        var predicate = spec.Criteria!.Compile();

        // Assert
        predicate(matchingProduct).Should().BeTrue();
        predicate(nonMatchingProduct).Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithPagingParameters_ShouldSetSkipAndTakeCorrectly()
    {
        // Arrange & Act
        var spec = new ProductsFilterSpecification(pageNumber: 3, pageSize: 15);

        // Assert
        spec.IsPagingEnabled.Should().BeTrue();
        spec.Skip.Should().Be(30); // (3 - 1) * 15
        spec.Take.Should().Be(15);
    }

    [Fact]
    public void Constructor_WithSortByPriceDescending_ShouldConfigureOrderByDescending()
    {
        // Arrange & Act
        var spec = new ProductsFilterSpecification(pageNumber: 1, pageSize: 10, sortBy: "price", isDescending: true);

        // Assert
        spec.OrderByDescending.Should().NotBeNull();
        spec.OrderBy.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptySearchTerm_ShouldMatchAllProducts()
    {
        // Arrange
        var spec = new ProductsFilterSpecification(pageNumber: 1, pageSize: 10, searchTerm: null);
        var product = new Product("Any Product", 100m, 1);

        // Act
        var predicate = spec.Criteria!.Compile();

        // Assert
        predicate(product).Should().BeTrue();
    }
}
