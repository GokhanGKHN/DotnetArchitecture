using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.WebApi.Services;

/// <summary>                                                                                                                                                                          
/// Harici ödeme sağlayıcısı API'sini (Stripe, İyzico vb.) ve farklı ağ senaryolarını                                                                                                  
/// (Normal, Flaky, Outage, Slow) simüle eden HTTP mesaj işleyicisi (DelegatingHandler).                                                                                               
/// </summary>                                                                                                                                                                         
public class PaymentApiSimulationHandler : DelegatingHandler
{
    private static int _flakyAttemptCounter = 0;
    private readonly ILogger<PaymentApiSimulationHandler> _logger;

    public PaymentApiSimulationHandler(ILogger<PaymentApiSimulationHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var mode = request.Headers.TryGetValues("X-Simulation-Mode", out var values)
            ? values.FirstOrDefault()?.ToLowerInvariant()
            : "normal";

        switch (mode)
        {
            case "flaky":
                var currentAttempt = Interlocked.Increment(ref _flakyAttemptCounter);
                if (currentAttempt % 3 != 0)
                {
                    _logger.LogWarning(
                        "💥 [DIŞ SERVİS SİMÜLATÖRÜ] Geçici ağ hatası simüle ediliyor! (Deneme: {Attempt} - HTTP 503 Service Unavailable)",
                        currentAttempt);

                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { error = "Geçici ödeme ağ geçidi hatası. Lütfen tekrar deneyin." }),
                            Encoding.UTF8,
                            "application/json")
                    };
                }

                _logger.LogInformation("✨ [DIŞ SERVİS SİMÜLATÖRÜ] 3. denemede servis toparlandı! (HTTP 200 OK)");
                return CreateSuccessResponse();

            case "outage":
                _logger.LogError("🔥 [DIŞ SERVİS SİMÜLATÖRÜ] Servis tamamen çökmüş durumda! (HTTP 503 Service Unavailable)");
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(new { error = "Ödeme sağlayıcısında genel kesinti var." }),
                        Encoding.UTF8,
                        "application/json")
                };

            case "slow":
                _logger.LogWarning("⏳ [DIŞ SERVİS SİMÜLATÖRÜ] Yavaş yanıt simülasyonu: 4 saniye bekleniyor (Timeout tetiklenecek)...");
                await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
                return CreateSuccessResponse();

            case "normal":
            default:
                _logger.LogInformation("🟢 [DIŞ SERVİS SİMÜLATÖRÜ] Ödeme isteği başarıyla karşılandı (HTTP 200 OK).");
                return CreateSuccessResponse();
        }
    }

    private static HttpResponseMessage CreateSuccessResponse()
    {
        var txnId = $"TXN-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var body = JsonSerializer.Serialize(new
        {
            transactionId = txnId,
            status = "Approved",
            processedAt = DateTime.UtcNow
        });

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
    }
}