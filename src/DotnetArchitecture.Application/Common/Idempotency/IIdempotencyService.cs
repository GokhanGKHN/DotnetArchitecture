namespace DotnetArchitecture.Application.Common.Idempotency;

/// <summary>
/// Mükerrer istekleri engellemek için anahtar durumunu denetleyen ve sonuçları saklayan servis arayüzü.
/// </summary>
public interface IIdempotencyService
{
    /// <summary>
    /// Belirtilen anahtarın durumunu denetler:
    /// - İlk kez geliyorsa: Kilidi alır ve FirstRun döner.
    /// - Daha önce tamamlanmışsa: AlreadyProcessed ve kayıtlı yanıtı döner.
    /// - Başka bir paralel istek yürütülüyorsa: InProgress döner.
    /// </summary>
    Task<IdempotencyCheckResult<TResponse>> CheckAsync<TResponse>(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Başarıyla tamamlanan işlemin nihai sonucunu saklar (varsayılan 24 saat).
    /// </summary>
    Task SaveResultAsync<TResponse>(Guid key, TResponse response, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// İşlem beklenmeyen bir hatayla sonuçlandığında kilidi serbest bırakır (tekrar denenebilmesi için).
    /// </summary>
    Task ReleaseAsync(Guid key, CancellationToken cancellationToken = default);
}
