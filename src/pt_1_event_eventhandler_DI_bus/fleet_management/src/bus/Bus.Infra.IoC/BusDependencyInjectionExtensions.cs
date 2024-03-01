using Bus.Domain;
using Bus.Infra.Bus.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;

namespace Bus.Infra.IoC;

public static class BusDependencyInjectionExtensions
{
    public static IServiceCollection RegisterRabbitMQBus(
        this IServiceCollection services, 
        RabbitMQSettings settings)
    {
        services.AddSingleton<IBus, RabbitMQBus>(factory => {
            var scopeFactory = factory.GetRequiredService<IServiceScopeFactory>();
            return new RabbitMQBus(settings, scopeFactory);
        });

        return services;
    }
}
