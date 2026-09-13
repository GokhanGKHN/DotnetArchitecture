namespace DotnetArchitecture.Application.Interfaces;

/// <summary>
/// Dağıtık veya yerel önbellekleme işlemlerini soyutlayan servis arayüzü.
/// Clean Architecture prensiplerine göre uygulama katmanı Redis veya MemoryCache gibi
/// altyapı detaylarına doğrudan bağımlı olmaz.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Verilen anahtarla eşleşen veriyi önbellekten getirir.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Veriyi belirtilen süreyle önbelleğe kaydeder.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen anahtara sahip önbellek kaydını siler.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
