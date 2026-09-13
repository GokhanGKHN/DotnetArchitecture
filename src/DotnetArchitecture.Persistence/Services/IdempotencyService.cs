using DotnetArchitecture.Application.Common.Idempotency;
using DotnetArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Persistence.Services;

/// <summary>
/// ICacheService (Redis veya Bellek) tabanlı dağıtık Idempotency servisi.
/// Mükerrer istekleri engeller, işlem devam ederken gelen paralel istekleri tespit eder (Conflict),
/// ve tamamlanan isteklerin sonucunu saklar (Replay).
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<IdempotencyService> _logger;

    private static readonly TimeSpan DefaultLockTimeout = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan DefaultResultRetention = TimeSpan.FromHours(24);

    public class IdempotencyRecord<T>
    {
        public bool IsCompleted { get; set; }
        public T? Response { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public IdempotencyService(
        ICacheService cacheService,
        ILogger<IdempotencyService> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    private static string GetCacheKey(Guid key) => $"idempotency:{key}";

    public async Task<IdempotencyCheckResult<TResponse>> CheckAsync<TResponse>(Guid key, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(key);
        var existingRecord = await _cacheService.GetAsync<IdempotencyRecord<TResponse>>(cacheKey, cancellationToken);

        if (existingRecord is not null)
        {
            // 1. İşlem daha önce başarıyla tamamlanmış: Kayıtlı sonucu döndür
            if (existingRecord.IsCompleted)
            {
                return new IdempotencyCheckResult<TResponse>(
                    IdempotencyStatus.AlreadyProcessed,
                    existingRecord.Response);
            }

            // 2. İşlem henüz tamamlanmamış ve kilit süresi dolmamış: Çakışma (Conflict)
            var elapsed = DateTime.UtcNow - existingRecord.CreatedAtUtc;
            if (elapsed < DefaultLockTimeout)
            {
                return new IdempotencyCheckResult<TResponse>(IdempotencyStatus.InProgress);
            }

            // Kilit süresi aşılmışsa (Örn: sunucu aniden kapandıysa) kilit düşmüş kabul edilir
            _logger.LogWarning("⚠️ [IDEMPOTENCY TIMEOUT] Anahtar için önceki işlem zaman aşımına uğradı, kilit yenileniyor: {IdempotencyKey}", key);
        }

        // 3. İlk kez geliyor veya kilit yenileniyor: In-flight kilidini koy
        var inFlightRecord = new IdempotencyRecord<TResponse>
        {
            IsCompleted = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, inFlightRecord, DefaultLockTimeout, cancellationToken);

        return new IdempotencyCheckResult<TResponse>(IdempotencyStatus.FirstRun);
    }

    public async Task SaveResultAsync<TResponse>(
        Guid key,
        TResponse response,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(key);
        var retention = expiration ?? DefaultResultRetention;

        var completedRecord = new IdempotencyRecord<TResponse>
        {
            IsCompleted = true,
            Response = response,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _cacheService.SetAsync(cacheKey, completedRecord, retention, cancellationToken);
    }

    public async Task ReleaseAsync(Guid key, CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(key);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }
}
