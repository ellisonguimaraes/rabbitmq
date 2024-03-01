using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MSRabbitMQ.Domain.Core.Bus;
using MSRabbitMQ.Domain.Core.Commands;
using MSRabbitMQ.Domain.Core.Events;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MSRabbitMQ.Infra.Bus;

public sealed class RabbitMQBus(IMediator mediator, IServiceScopeFactory serviceScopeFactory, IOptions<RabbitMQSettings> settings) : IEventBus
{
    private readonly RabbitMQSettings _settings = settings.Value;
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
    private readonly IMediator _mediator = mediator;
    private readonly Dictionary<string, List<Type>> _handlers = [];
    private readonly List<Type> _eventTypes = [];

    public void Publish<T>(T @event) where T : Event
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var eventName = @event.GetType().Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var message = JsonConvert.SerializeObject(@event);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(string.Empty, eventName, null, body);
    }

    public Task SendCommand<T>(T command) where T : Command
        => _mediator.Send(command);

    public void Subscribe<T, TH>()
        where T : Event
        where TH : IEventHandler<T>
    {
        var eventName = typeof(T).Name;
        var handlerType = typeof(TH);

        if (!_eventTypes.Contains(typeof(T)))
            _eventTypes.Add(typeof(T));

        if (!_handlers.ContainsKey(eventName))
            _handlers.Add(eventName, []);

        if (_handlers[eventName].Any(s => s.GetType().Equals(handlerType)))
            throw new ArgumentException($"Handler exception {handlerType.Name} was previously registered by '{eventName}'", nameof(handlerType));

        _handlers[eventName].Add(handlerType);

        StartBasicConsume<T>();
    }

    private void StartBasicConsume<T>() where T : Event
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        var eventName = typeof(T).Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += Consumer_Received;

        channel.BasicConsume(eventName, true, consumer);
    }

    private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
    {
        var eventName = @event.RoutingKey;
        var message = Encoding.UTF8.GetString(@event.Body.Span);

        try
        {
            await ProcessEvent(eventName, message).ConfigureAwait(false);
        }
        catch (Exception)
        {

        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (_handlers.ContainsKey(eventName))
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var subscriptions = _handlers[eventName];

                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetService(subscription);  //Activator.CreateInstance(subscription);
                    if (handler == null) continue;
                    var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                    var @event = JsonConvert.DeserializeObject(message, eventType);
                    var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);

                    await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });

                }

            }
        }
    }
}
