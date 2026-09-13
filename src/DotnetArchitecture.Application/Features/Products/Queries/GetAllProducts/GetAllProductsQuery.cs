using DotnetArchitecture.Application.Common;
using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

// Sayfalama, arama ve sıralama destekleyen ürün sorgusu.
// Sonuçlar PagedResponse<ProductResponse> olarak döner ve parametrelere göre önbelleğe alınır.
public record GetAllProductsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null,
    string? SortBy = null,
    bool IsDescending = false
) : IRequest<PagedResponse<ProductResponse>>, ICacheableQuery
{
    public string CacheKey => $"products-p{PageNumber}-s{PageSize}-q{SearchTerm ?? "all"}-by{SortBy ?? "default"}-desc{IsDescending}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}