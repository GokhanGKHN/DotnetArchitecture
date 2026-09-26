/// <summary>                                                                                                                                                                          
/// Harici ödeme sağlayıcısına gönderilecek ödeme isteği modeli.                                                                                                                       
/// </summary>                                                                                                                                                                         
public record PaymentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string CardNumberMasked,
    string? SimulationMode = null // "Normal", "Flaky", "Outage", "Slow"                                                                                                               
);

/// <summary>                                                                                                                                                                          
/// Harici ödeme sağlayıcısından dönen sonuç modeli.                                                                                                                                   
/// </summary>                                                                                                                                                                         
public record PaymentResult(
    bool IsSuccess,
    string? TransactionId,
    string? ErrorMessage,
    int AttemptCount = 1
);

/// <summary>                                                                                                                                                                          
/// Dış ödeme altyapısıyla (Stripe, İyzico vb.) haberleşen servis arayüzü.                                                                                                             
/// </summary>                                                                                                                                                                         
public interface IPaymentGateway
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}