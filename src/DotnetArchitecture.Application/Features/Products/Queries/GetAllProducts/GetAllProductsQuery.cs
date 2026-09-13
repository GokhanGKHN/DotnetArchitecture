using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

// Tüm ürünleri listeleyen sorgu. ICacheableQuery sayesinde sonucu önbelleğe alınır.
public record GetAllProductsQuery() : IRequest<IReadOnlyList<ProductResponse>>, ICacheableQuery
{
    public string CacheKey => "products-all";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(2);
}