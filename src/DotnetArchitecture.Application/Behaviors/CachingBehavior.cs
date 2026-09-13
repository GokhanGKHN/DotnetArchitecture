using DotnetArchitecture.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Behaviors;

/// <summary>
/// MediatR isteklerini araya girerek yakalayan ve ICacheService üzerinden dağıtık/yerel önbellek operasyonlarını yöneten turnike.
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cacheService, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
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

            var cachedResponse = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
            if (cachedResponse is not null)
            {
                _logger.LogInformation("⚡ [CACHE HIT] Veri önbellekten getirildi: {CacheKey}", cacheKey);
                return cachedResponse;
            }

            _logger.LogInformation("💨 [CACHE MISS] Önbellekte bulunamadı, veritabanından çekiliyor: {CacheKey}", cacheKey);

            // Handler çalıştırılır ve veritabanına gidilir
            var response = await next(cancellationToken);

            var expiration = cacheableQuery.Expiration ?? TimeSpan.FromMinutes(5);
            await _cacheService.SetAsync(cacheKey, response, expiration, cancellationToken);

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
                await _cacheService.RemoveAsync(key, cancellationToken);
                _logger.LogInformation("🧹 [CACHE INVALIDATED] Önbellek anahtarı temizlendi: {CacheKey}", key);
            }

            return response;
        }

        // İstek önbellekleme ile ilgili değilse akışa doğrudan devam et
        return await next(cancellationToken);
    }
}
