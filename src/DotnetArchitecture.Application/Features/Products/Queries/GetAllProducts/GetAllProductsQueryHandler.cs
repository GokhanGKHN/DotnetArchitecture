using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, IReadOnlyList<ProductResponse>>
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<IReadOnlyList<ProductResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAllAsync(cancellationToken);

        // Entity listesini DTO listesine dönüştürüyoruz (Mapping)                                                                                     
        return products.Select(p => new ProductResponse(
            p.Id,
            p.Name,
            p.Price,
            p.StockQuantity
        )).ToList();
    }
}
