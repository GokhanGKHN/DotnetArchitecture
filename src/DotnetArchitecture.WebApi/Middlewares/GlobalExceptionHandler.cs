using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DotnetArchitecture.WebApi.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Bir hata oluştu: {Message}", exception.Message);

        // 1. Eğer hata FluentValidation hatasıysa detaylı hata listesi dönüyoruz:                                                                                          
        if (exception is ValidationException validationException)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

            var validationProblemDetails = new HttpValidationProblemDetails(
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validasyon Hatası (Validation Failed)",
                Detail = "Gönderilen model kurallara uymuyor.",
                Instance = httpContext.Request.Path
            };

            await httpContext.Response.WriteAsJsonAsync(validationProblemDetails, cancellationToken);
            return true;
        }

        // 2. Diğer genel hatalar                                                                                                                                           
        var (statusCode, title) = exception switch
        {
            InvalidOperationException => (StatusCodes.Status400BadRequest, "İş Kuralı İhlali (Business Rule Violation)"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Geçersiz İstek (Bad Request)"),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Kayıt Bulunamadı (Not Found)"),
            _ => (StatusCodes.Status500InternalServerError, "Sunucu Hatası (Server Error)")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
