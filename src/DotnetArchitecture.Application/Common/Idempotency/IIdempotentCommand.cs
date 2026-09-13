using MediatR;

namespace DotnetArchitecture.Application.Common.Idempotency;

/// <summary>
/// Mükerrer çağrılarda aynı sonucu üretmesi gereken (Idempotent) komutların uyguladığı arayüz.
/// </summary>
public interface IIdempotentCommand<out TResponse> : IRequest<TResponse>
{
    /// <summary>
    /// İstemci tarafından sağlanan benzersiz istek anahtarı (örn: HTTP Idempotency-Key başlığı).
    /// Null ise idempotency turnikesi işletilmez ve komut normal akışta çalışır.
    /// </summary>
    Guid? IdempotencyKey { get; }
}
