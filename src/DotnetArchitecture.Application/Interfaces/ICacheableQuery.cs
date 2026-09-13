namespace DotnetArchitecture.Application.Interfaces;

/// <summary>
/// Sonuçları önbelleğe alınabilir olan sorguların (Query) uyguladığı arayüz.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Önbellek anahtarı (örn: "products-all", "order-89605202-...")
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Verinin önbellekte kalma süresi (null ise varsayılan süre kullanılır).
    /// </summary>
    TimeSpan? Expiration { get; }
}
