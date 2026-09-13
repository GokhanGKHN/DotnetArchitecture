using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace DotnetArchitecture.Application.UnitTests.Features.Products;

public class CreateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProductCommandHandler(_productRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenValidCommand_ShouldSaveProductAndReturnId()
    {
        // Arrange
        var command = new CreateProductCommand("Ergonomic Chair", 4500, 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();

        await _productRepository.Received(1).AddAsync(
            Arg.Is<Product>(p => p.Name == "Ergonomic Chair" && p.Price == 4500 && p.StockQuantity == 10),
            Arg.Any<CancellationToken>());

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
