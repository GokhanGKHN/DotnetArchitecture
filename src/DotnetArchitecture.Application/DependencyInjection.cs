using DotnetArchitecture.Application.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetArchitecture.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // 1. In-Memory Cache servisi
        services.AddMemoryCache();

        // 2. MediatR ve Pipeline Turnikeleri
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Turnike 1: Kronometre başlasın (En dış halka - tüm süreyi ölçer)
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));

            // Turnike 2: Önbellek kontrolü (Cache varsa anında döner, DB ve validasyona gitmez)
            cfg.AddOpenBehavior(typeof(CachingBehavior<,>));

            // Turnike 3: Validasyon denetimi (Sadece cache miss olduğunda veya komutlarda çalışır)
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });


        // 2. Bu katmandaki tüm FluentValidation Validator sınıflarını otomatik bul ve kaydet                                                                               
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

