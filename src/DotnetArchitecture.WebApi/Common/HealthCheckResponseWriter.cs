using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotnetArchitecture.WebApi.Common;

/// <summary>
/// Health Check sonuçlarını kurumsal izleme araçlarının (Prometheus, Datadog, Grafana)
/// anlayabileceği standart JSON formatına dönüştüren yanıt yazıcı.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task WriteDetailedResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestampUtc = DateTime.UtcNow,
            entries = report.Entries.Select(e => new
            {
                component = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? (e.Value.Status == HealthStatus.Healthy ? "Bileşen sorunsuz çalışıyor." : "Bileşende sorun tespit edildi."),
                durationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                tags = e.Value.Tags,
                error = e.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
