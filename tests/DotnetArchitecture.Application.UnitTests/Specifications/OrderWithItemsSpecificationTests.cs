using DotnetArchitecture.Application.Features.Orders.Specifications;
using DotnetArchitecture.Domain.Entities;
using FluentAssertions;

namespace DotnetArchitecture.Application.UnitTests.Specifications;

public class OrderWithItemsSpecificationTests
{
    [Fact]
    public void Constructor_WithOrderId_ShouldSetCriteriaAndIncludeItems()
    {
        // Arrange
        var targetOrderId = Guid.NewGuid();
        var spec = new OrderWithItemsSpecification(targetOrderId);

        var matchingOrder = new Order(Guid.NewGuid());
        // Reflection ile Id set ediyoruz
        typeof(Order).GetProperty(nameof(Order.Id))!.SetValue(matchingOrder, targetOrderId);

        var nonMatchingOrder = new Order(Guid.NewGuid());

        // Act
        var predicate = spec.Criteria!.Compile();

        // Assert
        predicate(matchingOrder).Should().BeTrue();
        predicate(nonMatchingOrder).Should().BeFalse();
        spec.Includes.Should().HaveCount(1);
    }
}
