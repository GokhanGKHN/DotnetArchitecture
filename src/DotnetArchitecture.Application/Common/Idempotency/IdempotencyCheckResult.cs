namespace DotnetArchitecture.Application.Common.Idempotency;

/// <summary>
/// Idempotency anahtarının durumunu belirten durumlar.
/// </summary>
public enum IdempotencyStatus
{
    /// <summary>
    /// Bu anahtar ilk kez geldi, kilit alındı ve işlem yürütülebilir.
    /// </summary>
    FirstRun,

    /// <summary>
    /// Bu anahtar daha önce başarıyla tamamlandı, kayıtlı yanıt doğrudan döndürülebilir.
    /// </summary>
    AlreadyProcessed,

    /// <summary>
    /// Bu anahtar şu an başka bir istek tarafından işleniyor (Eşzamanlı çakışma / 409 Conflict).
    /// </summary>
    InProgress
}

/// <summary>
/// Idempotency kontrolünün sonucunu ve varsa kayıtlı yanıtı tutan kayıt.
/// </summary>
public record IdempotencyCheckResult<TResponse>(
    IdempotencyStatus Status,
    TResponse? StoredResponse = default
);
