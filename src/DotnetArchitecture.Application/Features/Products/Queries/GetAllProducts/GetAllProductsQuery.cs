using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

// Tüm ürünleri listeleyen sorgu. Geriye ProductResponse listesi döner.                                                                                
public record GetAllProductsQuery() : IRequest<IReadOnlyList<ProductResponse>>;