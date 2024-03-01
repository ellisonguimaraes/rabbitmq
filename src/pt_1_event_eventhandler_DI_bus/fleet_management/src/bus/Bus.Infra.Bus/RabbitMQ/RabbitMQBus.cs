using System.Text;
using Bus.Domain;
using Bus.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bus.Infra.Bus.RabbitMQ;

public class RabbitMQBus(
    RabbitMQSettings settings,
    IServiceScopeFactory serviceScopeFactory) : IBus
{   
    #region Dependency Injection
    private readonly RabbitMQSettings _settings = settings;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    #endregion

    private Dictionary<string, List<Type>> _handlers = [];
    private List<Type> _eventTypes = [];

    public void Publish<T>(T @event) where T : Event
    {
        var factory = CreateConnectionFactory();

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var eventName = @event.GetType().Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var message = JsonConvert.SerializeObject(@event);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(string.Empty, eventName, null, body);
    }

    public void Subscribe<T, THandler>()
        where T : Event
        where THandler : IEventHandler<T>
    {
        var eventType = typeof(T);
        var handlerType = typeof(THandler);

        if (!_eventTypes.Contains(eventType)) _eventTypes.Add(eventType);

        if (!_handlers.ContainsKey(eventType.Name)) _handlers.Add(eventType.Name, []);

        if (_handlers[eventType.Name].Any(s => s.GetType().Equals(handlerType)))
            throw new ArgumentException($"Handler exception {handlerType.Name} was previously registered by '{eventType.Name}'", nameof(handlerType));

        _handlers[eventType.Name].Add(handlerType);

        StartBasicConsume<T>();
    }

    private void StartBasicConsume<T>() where T : Event
    {
        var factory = CreateConnectionFactory();

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        var eventName = typeof(T).Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (sender, @event) => {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
                channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception)
            {
                channel.BasicNack(@event.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(eventName, false, consumer);
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (_handlers.TryGetValue(eventName, out List<Type>? subscriptions))
        {
            using var scope = _serviceScopeFactory.CreateScope();

            foreach (var sub in subscriptions)
            {
                var handler = scope.ServiceProvider.GetService(sub);
                if (handler is null) continue;

                var eventType = _eventTypes.Single(t => t.Name.Equals(eventName));
                var @event = JsonConvert.DeserializeObject(message, eventType);
                var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                
                await (Task)concreteType.GetMethod("Handle")!.Invoke(handler, [@event!])!;
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory()
        => new () {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };
}
