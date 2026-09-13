using DotnetArchitecture.Application.Common.Specifications;
using Microsoft.EntityFrameworkCore;

namespace DotnetArchitecture.Persistence.Specifications;

/// <summary>
/// ISpecification kurallarını Entity Framework Core'un IQueryable sorgu ağacına dönüştüren değerlendirici (Evaluator).
/// </summary>
public static class SpecificationEvaluator<T> where T : class
{
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> spec)
    {
        var query = inputQuery;

        // 1. Filtreleme (WHERE kriteri)
        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }

        // 2. Eager Loading (Expression tabanlı Include)
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // 3. Eager Loading (Dize tabanlı Include)
        query = spec.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // 4. Sıralama (ORDER BY / ORDER BY DESCENDING)
        if (spec.OrderBy != null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        // 5. Veritabanı Düzeyinde Sayfalama (OFFSET / FETCH NEXT)
        if (spec.IsPagingEnabled)
        {
            query = query.Skip(spec.Skip).Take(spec.Take);
        }

        return query;
    }
}
