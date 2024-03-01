using System.Reflection;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MSRabbitMQ.Domain.Core.Bus;
using MSRabbitMQ.Infra.Bus;

namespace MSRabbitMQ.Infra.IoC;

public static class DependencyContainer
{
    public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Mediator
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Domain Bus
        services.AddSingleton<IEventBus, RabbitMQBus>(sp => { 
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var optionsFactory = sp.GetService<IOptions<RabbitMQSettings>>();
            return new RabbitMQBus(sp.GetService<IMediator>(), scopeFactory, optionsFactory);
        });
        
        return services;
    }
}
