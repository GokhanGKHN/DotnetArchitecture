using System.Net.Http.Json;
using System.Text.Json;
using DotnetArchitecture.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace DotnetArchitecture.WebApi.Services;

/// <summary>                                                                                                                                                                          
/// Harici ödeme sağlayıcısı ile konuşan, Polly v8 standart dayanıklılık                                                                                                               
/// boru hattı (Resilience Pipeline) ile korunan HTTP istemcisi.                                                                                                                       
/// </summary>                                                                                                                                                                         
public class PaymentGatewayClient : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentGatewayClient> _logger;

    public PaymentGatewayClient(HttpClient httpClient, ILogger<PaymentGatewayClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/payments")
            {
                Content = JsonContent.Create(new
                {
                    orderId = request.OrderId,
                    amount = request.Amount,
                    currency = request.Currency,
                    cardNumberMasked = request.CardNumberMasked
                })
            };

            if (!string.IsNullOrWhiteSpace(request.SimulationMode))
            {
                httpRequest.Headers.Add("X-Simulation-Mode", request.SimulationMode);
            }

            _logger.LogInformation(
                "💳 [ÖDEME İSTEMCİSİ] Harici ödeme servisine istek gönderiliyor... Sipariş ID: {OrderId}, Tutar: {Amount} {Currency}, Mod: {Mode}",
                request.OrderId, request.Amount, request.Currency, request.SimulationMode ?? "Normal");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                var txnId = content.GetProperty("transactionId").GetString();

                _logger.LogInformation("✅ [ÖDEME BAŞARILI] İşlem onaylandı! İşlem No: {TxnId}", txnId);
                return new PaymentResult(true, txnId, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("❌ [ÖDEME REDDEDİLDİ] Dış servis hata döndü: {StatusCode} - {Error}", response.StatusCode, errorBody);
            return new PaymentResult(false, null, $"Dış servis hatası ({response.StatusCode}): {errorBody}");
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "⚡ [DEVRE KESİCİ ENGELİ] Sigorta AÇIK (Circuit Breaker OPEN)! Dış servis kapalı olduğu için istek doğrudan reddedildi.");
            return new PaymentResult(false, null, "Ödeme altyapısı geçici olarak hizmet dışı (Circuit Breaker devrede). Lütfen birkaç saniye sonra tekrar deneyiniz.");
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex, "⏳ [ZAMAN AŞIMI ENGELİ] Ödeme sağlayıcısı zamanında yanıt vermedi (Timeout).");
            return new PaymentResult(false, null, "Ödeme işlemi zaman aşımına uğradı. Lütfen tekrar deneyiniz.");
        }
    }
}
