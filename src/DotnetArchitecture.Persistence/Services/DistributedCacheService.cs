using System.Text.Json;
using DotnetArchitecture.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Persistence.Services;

/// <summary>
/// IDistributedCache (Redis veya Dağıtık Bellek) üzerinden çalışan kurumsal önbellek servisi.
/// Olası ağ veya Redis kesintilerinde uygulamanın çökmesini engelleyen hata toleransı (Resilience / Graceful Degradation) içerir.
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DistributedCacheService(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var cachedBytes = await _distributedCache.GetAsync(key, cancellationToken);
            if (cachedBytes is null || cachedBytes.Length == 0)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedBytes, SerializerOptions);
        }
        catch (Exception ex)
        {
            // Redis bağlantı hatası veya kesinti olsa dahi iş akışını bozmayıp Cache Miss gibi davranır (Graceful Degradation)
            _logger.LogWarning(ex, "⚠️ [DISTRIBUTED CACHE] Önbellekten okuma sırasında hata oluştu: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
            };

            await _distributedCache.SetAsync(key, bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [DISTRIBUTED CACHE] Önbelleğe yazma sırasında hata oluştu: {CacheKey}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [DISTRIBUTED CACHE] Önbellekten silme sırasında hata oluştu: {CacheKey}", key);
        }
    }
}
