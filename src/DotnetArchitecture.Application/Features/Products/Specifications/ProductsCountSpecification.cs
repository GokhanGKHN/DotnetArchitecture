using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Domain.Entities;

namespace DotnetArchitecture.Application.Features.Products.Specifications;

/// <summary>
/// Toplam ürün sayısını filtreleme kriterine göre hesaplayan (sayfalama ve sıralama içermeyen) Specification.
/// </summary>
public class ProductsCountSpecification : BaseSpecification<Product>
{
    public ProductsCountSpecification(string? searchTerm = null)
        : base(p => string.IsNullOrWhiteSpace(searchTerm) || p.Name.Contains(searchTerm.Trim()))
    {
    }
}
