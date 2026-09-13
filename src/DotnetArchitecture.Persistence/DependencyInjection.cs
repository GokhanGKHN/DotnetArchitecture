using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Persistence.Context;
using DotnetArchitecture.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetArchitecture.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        // 0. SaveChanges Interceptor (Audit & Soft Delete)
        services.AddScoped<Interceptors.AuditableEntityInterceptor>();

        // 1. SQL Server bağlantısı
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<Interceptors.AuditableEntityInterceptor>();
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                   .AddInterceptors(interceptor);
        });

        // 2. Repository ve Unit of Work kayıtları (Scoped: Her HTTP isteğinde tek bir örnek)
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // 3. Kimlik Doğrulama Servisleri (Password Hasher & JWT Generator)
        services.AddSingleton<IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, Services.JwtTokenGenerator>();

        // 4. Dağıtık Önbellekleme Servisleri (Redis veya Fallback Memory)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "DotnetArch_";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, Services.DistributedCacheService>();

        // 5. Dağıtık Idempotency Servisi
        services.AddSingleton<DotnetArchitecture.Application.Common.Idempotency.IIdempotencyService, Services.IdempotencyService>();

        return services;
    }
}
