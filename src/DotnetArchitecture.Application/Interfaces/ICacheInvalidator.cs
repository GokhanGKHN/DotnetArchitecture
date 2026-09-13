namespace DotnetArchitecture.Application.Interfaces;

/// <summary>
/// Başarıyla tamamlandığında belirli önbellek anahtarlarını temizlemesi gereken komutların (Command) uyguladığı arayüz.
/// </summary>
public interface ICacheInvalidator
{
    /// <summary>
    /// Silinmesi gereken önbellek anahtarları (örn: ["products-all"]).
    /// </summary>
    IReadOnlyCollection<string> CacheKeysToInvalidate { get; }
}
