namespace DotnetArchitecture.Application.Common.Exceptions;

/// <summary>
/// Aynı Idempotency-Key ile eşzamanlı devam eden bir işlem varken yeni bir istek geldiğinde fırlatılan istisna (HTTP 409 Conflict).
/// </summary>
public class IdempotencyConflictException : Exception
{
    public Guid IdempotencyKey { get; }

    public IdempotencyConflictException(Guid idempotencyKey)
        : base($"Bu işlem ('{idempotencyKey}') şu anda başka bir paralel istek tarafından işleniyor. Lütfen bekleyiniz.")
    {
        IdempotencyKey = idempotencyKey;
    }
}
