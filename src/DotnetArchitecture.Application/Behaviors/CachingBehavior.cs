using DotnetArchitecture.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Durum: İstek bir ICacheableQuery ise önce önbelleğe bak
        if (request is ICacheableQuery cacheableQuery)
        {
            var cacheKey = cacheableQuery.CacheKey;

            if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse) && cachedResponse is not null)
            {
                _logger.LogInformation("⚡ [CACHE HIT] Veri önbellekten getirildi: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            _logger.LogInformation("💨 [CACHE MISS] Önbellekte bulunamadı, veritabanından çekiliyor: {CacheKey}", cacheKey);

            // Handler çalıştırılır ve veritabanına gidilir
            var response = await next(cancellationToken);

            var expiration = cacheableQuery.Expiration ?? TimeSpan.FromMinutes(5);
            _cache.Set(cacheKey, response, expiration);

            _logger.LogInformation("💾 [CACHE SET] Veri önbelleğe yazıldı ({Expiration} süreyle): {CacheKey}", expiration, cacheKey);

            return response;
        }

        // 2. Durum: İstek bir ICacheInvalidator ise (örn: CreateProductCommand)
        if (request is ICacheInvalidator invalidator)
        {
            // Önce komut çalışsın ve veritabanı işlemi tamamlansın
            var response = await next(cancellationToken);

            // Başarılı olduktan sonra ilgili önbellek anahtarlarını temizle
            foreach (var key in invalidator.CacheKeysToInvalidate)
            {
                _cache.Remove(key);
                _logger.LogInformation("🧹 [CACHE INVALIDATED] Önbellek anahtarı temizlendi: {CacheKey}", key);
            }

            return response;
        }

        // İstek önbellekleme ile ilgili değilse akışa doğrudan devam et
        return await next(cancellationToken);
    }
}
