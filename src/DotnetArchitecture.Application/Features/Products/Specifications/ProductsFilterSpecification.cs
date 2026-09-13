using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Domain.Entities;

namespace DotnetArchitecture.Application.Features.Products.Specifications;

/// <summary>
/// Ürünleri arama terimi, sıralama ve sayfalama parametrelerine göre sorgulayan Specification.
/// </summary>
public class ProductsFilterSpecification : BaseSpecification<Product>
{
    public ProductsFilterSpecification(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool isDescending = false)
        : base(p => string.IsNullOrWhiteSpace(searchTerm) || p.Name.Contains(searchTerm.Trim()))
    {
        // 1. Dinamik Sıralama (Sorting)
        switch (sortBy?.ToLower())
        {
            case "price":
                if (isDescending) ApplyOrderByDescending(p => p.Price);
                else ApplyOrderBy(p => p.Price);
                break;
            case "stock":
                if (isDescending) ApplyOrderByDescending(p => p.StockQuantity);
                else ApplyOrderBy(p => p.StockQuantity);
                break;
            case "name":
                if (isDescending) ApplyOrderByDescending(p => p.Name);
                else ApplyOrderBy(p => p.Name);
                break;
            default:
                if (isDescending) ApplyOrderByDescending(p => p.CreatedAtUtc);
                else ApplyOrderBy(p => p.CreatedAtUtc);
                break;
        }

        // 2. Veritabanı Düzeyinde Sayfalama (Paging - Skip & Take)
        var skip = (pageNumber - 1) * pageSize;
        ApplyPaging(skip, pageSize);
    }
}
