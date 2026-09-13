using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Domain.Enums;
using DotnetArchitecture.Domain.Events;
using FluentAssertions;
using Xunit;

namespace DotnetArchitecture.Domain.UnitTests.Entities;

public class OrderTests
{
    [Fact]
    public void AddItem_WhenValidProductAndQuantity_ShouldDeductStockAndRaiseOrderCreatedDomainEvent()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var order = new Order(customerId);
        var product = new Product("Mechanical Keyboard", 3000, 10);

        // Act
        order.AddItem(product, 2);

        // Assert
        order.Status.Should().Be(OrderStatus.Pending);
        order.Items.Should().HaveCount(1);
        order.TotalAmount.Should().Be(6000);
        product.StockQuantity.Should().Be(8);

        // Domain Event kontrolü
        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedDomainEvent>()
            .Which.TotalAmount.Should().Be(6000);
    }

    [Fact]
    public void Cancel_WhenOrderIsPending_ShouldChangeStatusToCancelled()
    {
        // Arrange
        var order = new Order(Guid.NewGuid());

        // Act
        order.Cancel();

        // Assert
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var order = new Order(Guid.NewGuid());
        order.Cancel();

        // Act
        var act = () => order.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Sipariş zaten iptal edilmiş.*");
    }
}
