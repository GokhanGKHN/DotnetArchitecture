using System.Text;
using DotnetArchitecture.Application;
using DotnetArchitecture.Persistence;
using DotnetArchitecture.WebApi.Common;
using DotnetArchitecture.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Katman servisleri
builder.Services.AddApplicationServices();
builder.Services.AddPersistenceServices(builder.Configuration);

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

// 5. Health Checks (Sağlık Denetimleri) Kaydı
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("API süreci aktif ve çalışıyor."), tags: ["live"])
    .AddDbContextCheck<DotnetArchitecture.Persistence.Context.AppDbContext>(
        name: "sqlserver",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["ready", "db"]);

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

