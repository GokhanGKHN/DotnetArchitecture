using System.Linq.Expressions;

namespace DotnetArchitecture.Application.Common.Specifications;

/// <summary>
/// Domain-Driven Design (DDD) Specification Deseni Arayüzü.
/// Sorgu kurallarını (Filtre, Sıralama, İlişkiler ve Sayfalama) kapsülleyen standart kontrat.
/// </summary>
public interface ISpecification<T>
{
    /// <summary>
    /// WHERE koşul ifadesi (Filtreleme kriteri).
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Sorguya dahil edilecek ilişkili varlıklar (Eager Loading - Include).
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Dize formatındaki ilişkiler (örn: "Items.Product").
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Artan sıralama ifadesi (ORDER BY).
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Azalan sıralama ifadesi (ORDER BY DESCENDING).
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Sayfalama için atlanacak kayıt adedi (OFFSET / SKIP).
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Sayfalama için alınacak kayıt adedi (FETCH NEXT / TAKE).
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Sayfalama uygulanıp uygulanmayacağını belirten bayrak.
    /// </summary>
    bool IsPagingEnabled { get; }
}
