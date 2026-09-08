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
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>)); // ✨ Turnikeyi MediatR'a bağlıyoruz                                                                        
        });

        // 2. Bu katmandaki tüm FluentValidation Validator sınıflarını otomatik bul ve kaydet                                                                               
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

