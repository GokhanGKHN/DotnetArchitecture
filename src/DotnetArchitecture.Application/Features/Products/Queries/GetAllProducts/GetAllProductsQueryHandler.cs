using DotnetArchitecture.Application.Common;
using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

public class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, PagedResponse<ProductResponse>>
{
    private readonly IProductRepository _productRepository;

    public GetAllProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<PagedResponse<ProductResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken)
    {
        var (products, totalCount) = await _productRepository.GetPagedAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            searchTerm: request.SearchTerm,
            sortBy: request.SortBy,
            isDescending: request.IsDescending,
            cancellationToken: cancellationToken);

        // Entity listesini DTO listesine dönüştürüyoruz
        var items = products.Select(p => new ProductResponse(
            p.Id,
            p.Name,
            p.Price,
            p.StockQuantity
        )).ToList();

        return PagedResponse<ProductResponse>.Create(
            items: items,
            totalCount: totalCount,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize
        );
    }
}
