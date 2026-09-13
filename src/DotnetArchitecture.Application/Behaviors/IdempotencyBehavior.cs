using DotnetArchitecture.Application.Common.Exceptions;
using DotnetArchitecture.Application.Common.Idempotency;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Behaviors;

/// <summary>
/// Mükerrer komut çağrılarının (aynı Idempotency-Key ile) veritabanında çift kayıt oluşturmasını
/// veya iş kurallarını birden çok kez çalıştırmasını önleyen MediatR turnikesi.
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyService idempotencyService,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. İstek IIdempotentCommand arayüzünü uyguluyorsa ve geçerli bir anahtar içeriyorsa turnikeye gir
        if (request is IIdempotentCommand<TResponse> idempotentCommand && idempotentCommand.IdempotencyKey.HasValue)
        {
            var key = idempotentCommand.IdempotencyKey.Value;
            var check = await _idempotencyService.CheckAsync<TResponse>(key, cancellationToken);

            // A. Durum: İstek daha önce başarıyla tamamlanmış (Replay)
            if (check.Status == IdempotencyStatus.AlreadyProcessed)
            {
                _logger.LogInformation("🔁 [IDEMPOTENT REPLAY] İstek daha önce işlenmiş. Kayıtlı yanıt döndürülüyor: {IdempotencyKey}", key);
                return check.StoredResponse!;
            }

            // B. Durum: İstek şu anda paralel bir iş parçacığı veya başka bir API isteği tarafından yürütülüyor (Conflict)
            if (check.Status == IdempotencyStatus.InProgress)
            {
                _logger.LogWarning("⚠️ [IDEMPOTENCY CONFLICT] Bu anahtarla paralel bir işlem yürütülüyor: {IdempotencyKey}", key);
                throw new IdempotencyConflictException(key);
            }

            // C. Durum: İlk kez geliyor (FirstRun). Kilidi aldık, işlemi yürüt
            _logger.LogInformation("🔒 [IDEMPOTENCY LOCK ACQUIRED] Anahtar için kilit alındı, işlem yürütülüyor: {IdempotencyKey}", key);

            try
            {
                var response = await next(cancellationToken);
                await _idempotencyService.SaveResultAsync(key, response, cancellationToken: cancellationToken);
                _logger.LogInformation("💾 [IDEMPOTENCY SAVED] İşlem başarıyla tamamlandı ve sonuç saklandı: {IdempotencyKey}", key);
                return response;
            }
            catch (Exception)
            {
                // İşlem beklenmeyen bir hatayla sonlanırsa kilidi kaldır ki kullanıcı düzelttikten sonra tekrar deneyebilsin
                await _idempotencyService.ReleaseAsync(key, CancellationToken.None);
                throw;
            }
        }

        // İstek idempotent değilse veya anahtarsızsa akışa doğrudan devam et
        return await next(cancellationToken);
    }
}
