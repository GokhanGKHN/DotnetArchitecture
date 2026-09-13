using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Behaviors;

public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;

    public PerformanceBehavior(ILogger<TRequest> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("🚀 [İŞLEM BAŞLADI] İstek: {RequestName}", requestName);

        _timer.Start();

        var response = await next(cancellationToken);

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        // 500 ms'den uzun süren istekler için uyarı logu                                                                                                                   
        if (elapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "⚠️ [YAVAŞ İSTEK TESPİT EDİLDİ] {RequestName} işlemi {ElapsedMilliseconds} ms sürdü! İnceleme gerekebilir.",
                requestName, elapsedMilliseconds);
        }
        else
        {
            _logger.LogInformation(
                "✅ [İŞLEM TAMAMLANDI] {RequestName} işlemi {ElapsedMilliseconds} ms içinde tamamlandı.",
                requestName, elapsedMilliseconds);
        }

        return response;
    }
}
