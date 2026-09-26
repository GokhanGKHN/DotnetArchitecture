using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.WebApi.Services;
using Microsoft.Extensions.Http.Resilience;
using MassTransit;
using System.Text;
using System.Threading.RateLimiting;
using DotnetArchitecture.Application;
using DotnetArchitecture.Persistence;
using DotnetArchitecture.WebApi.Common;
using DotnetArchitecture.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Katman servisleri
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<DotnetArchitecture.Application.Interfaces.ICurrentUserService, DotnetArchitecture.WebApi.Services.CurrentUserService>();
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);
// 2. MassTransit & RabbitMQ Yapılandırması                                                                                                             
builder.Services.AddMassTransit(x =>
{
    // Application katmanındaki tüm Consumer sınıflarını otomatik bul ve kaydet
    x.AddConsumers(typeof(DotnetArchitecture.Application.DependencyInjection).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqConn = builder.Configuration.GetConnectionString("RabbitMq")
                           ?? "amqp://guest:guest@localhost:5672";

        cfg.Host(new Uri(rabbitMqConn));

        // Kuyrukları ve endpoint'leri MassTransit otomatik isimlendirip bağlasın
        cfg.ConfigureEndpoints(context);
    });
});


// 2. Global Exception Handler & ProblemDetails kayıtları
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// 3. Arka Plan Servisleri (Hosted Services / Workers)
builder.Services.AddHostedService<DotnetArchitecture.WebApi.BackgroundServices.ProcessOutboxMessagesBackgroundService>();

// 4. JWT Kimlik Doğrulama & Yetkilendirme Yapılandırması
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "SuperSecretKeyForDotnetArchitectureDemo2026!WithEnoughBits";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "DotnetArchitectureApi",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "DotnetArchitectureClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});
builder.Services.AddAuthorization();

// 5. Dahili Rate Limiter (İstek Sınırlama Turnikesi)
builder.Services.AddRateLimiter(options =>
{
    // A. 429 Yanıtını RFC 7807/9110 ProblemDetails formatında özelleştiriyoruz
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too Many Requests",
            Type = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
            Detail = "Çok fazla istek gönderdiniz. Lütfen bir süre bekleyip tekrar deneyiniz.",
            Instance = context.HttpContext.Request.Path
        };

        await context.HttpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: token);
    };

    // B. Auth Politikası: IP başına dakikada maksimum 10 istek (Brute-force koruması)
    options.AddPolicy("AuthPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    // C. Genel API Politikası: IP başına dakikada maksimum 100 istek
    options.AddPolicy("GeneralPolicy", httpContext =>
    {
        var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: clientIp,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 5
            });
    });
});

// 6. Health Checks (Sağlık Denetimleri) Kaydı
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API süreci aktif ve çalışıyor."), tags: ["live"])
    .AddDbContextCheck<DotnetArchitecture.Persistence.Context.AppDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    healthChecksBuilder.AddRedis(
        redisConnectionString: redisConnectionString,
        name: "redis",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready", "cache"]);
}
// 7. Harici Ödeme Servisi & Polly Dayanıklılık Boru Hattı (Resilience Pipeline)                                                                                                       
builder.Services.AddTransient<PaymentApiSimulationHandler>();
builder.Services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client =>
{
 client.BaseAddress = new Uri("https://api.external-payment-gateway.com/");
})
.AddHttpMessageHandler<PaymentApiSimulationHandler>()
.AddStandardResilienceHandler(options =>
{
    // A. Akıllı Yeniden Deneme (Retry): Geçici 5xx ve 408 hatalarında 3 kez üstel artış (Exponential Backoff + Jitter) ile tekrar dene                                                
 options.Retry.MaxRetryAttempts = 3;
 options.Retry.Delay = TimeSpan.FromMilliseconds(200);
 options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
 options.Retry.UseJitter = true;
 options.Retry.OnRetry = args =>
 {
     Console.WriteLine($"⚠️ [POLLY RETRY] İstek başarısız oldu ({args.Outcome.Result?.StatusCode}). {args.AttemptNumber}. deneme yapılıyor...");
     return ValueTask.CompletedTask;
 };

    // B. Devre Kesici (Circuit Breaker): 10 saniyede %50'den fazla hata olursa devreyi 15 saniyeliğine AÇ (OPEN)!                                                                     
 options.CircuitBreaker.FailureRatio = 0.5;
 options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
 options.CircuitBreaker.MinimumThroughput = 4;
 options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
 options.CircuitBreaker.OnOpened = args =>
 {
     Console.WriteLine($"⚡ [CIRCUIT BREAKER] Sigorta attı! Devre AÇILDI (OPEN). Hata oranı aşıldı, dış servise istekler {args.BreakDuration.TotalSeconds} saniye kesildi.");
     return ValueTask.CompletedTask;
 };
 options.CircuitBreaker.OnClosed = args =>
 {
     Console.WriteLine("✅ [CIRCUIT BREAKER] Sigorta kapandı! Devre KAPALI (CLOSED). Dış servis toparlandı, istekler normale döndü.");
     return ValueTask.CompletedTask;
 };
 options.CircuitBreaker.OnHalfOpened = args =>
 {
     Console.WriteLine("🔄 [CIRCUIT BREAKER] Devre YARI AÇIK (HALF-OPEN). Dış servisin düzelip düzelmediği test ediliyor...");
     return ValueTask.CompletedTask;
 };

    // C. İstek Başına Zaman Aşımı (Attempt Timeout): Dış servis 2 saniye içinde yanıt vermezse zaman aşımına uğrat                                                                    
 options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Exception Handler Middleware'i en başta devreye alıyoruz!
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// Rate Limiter turnikesi kimlik doğrulamadan ÖNCE çalışarak sunucu kaynaklarını korur!
app.UseRateLimiter();

// Kimlik doğrulama turnikesi yetkilendirmeden ÖNCE çalışmalıdır!
app.UseAuthentication();
app.UseAuthorization();

// 6. Health Checks Uç Noktaları
// A. Kapsamlı JSON Sağlık Raporu (Tüm bileşenler)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
});

// B. Canlılık Probu (Liveness Probe - Container orkestrasyonu için)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

// C. Hazırlık Probu (Readiness Probe - Veritabanı ve bağımlılıklar için)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapControllers();

// Sadece gerçek SQL Server bağlantısı varsa bekleyen migration'ları otomatik uygula (Testlerde InMemory kullanılır)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DotnetArchitecture.Persistence.Context.AppDbContext>();
    if (Microsoft.EntityFrameworkCore.SqlServerDatabaseFacadeExtensions.IsSqlServer(dbContext.Database))
    {
        await Microsoft.EntityFrameworkCore.RelationalDatabaseFacadeExtensions.MigrateAsync(dbContext.Database);
    }
}

app.Run();

public partial class Program { }

