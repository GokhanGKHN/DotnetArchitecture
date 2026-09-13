using System.Text;
using DotnetArchitecture.Application;
using DotnetArchitecture.Persistence;
using DotnetArchitecture.WebApi.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

