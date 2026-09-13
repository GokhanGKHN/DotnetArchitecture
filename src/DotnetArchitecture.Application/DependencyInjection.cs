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

        // 1. MediatR ve ValidationBehavior kaydı                                                                                                                           
        services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);

                // 1. Önce kronometre başlasın (En dış halka)
                cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));

                // 2. Sonra validasyon turnikesi çalışsın (İç halka)
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });


        // 2. Bu katmandaki tüm FluentValidation Validator sınıflarını otomatik bul ve kaydet                                                                               
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

