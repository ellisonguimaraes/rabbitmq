using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPCServer.Domain;
using RPCServer.Services;

public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        var channel = SetupChannel(connection);

        InitializerConsumer(channel);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }
    
    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: nameof(Order),
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        channel.BasicQos(0, 1, false);

        return channel;
    }

    private static void InitializerConsumer(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var incommingMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"{DateTime.Now:o} Incomming -> {incommingMessage}");

                var order = JsonSerializer.Deserialize<Order>(incommingMessage);
                order!.SetStatus(ProccessOrderStatus(order.Amount));

                var replyMessage = JsonSerializer.Serialize(order);
                Console.WriteLine($"{DateTime.Now:o} Reply -> {replyMessage}");

                SendReplyMessage(replyMessage, channel, ea);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {
                // Efetuar log
                // Pode também retornar mensagem com status de erro.
            }
        };

        channel.BasicConsume(
            queue: nameof(Order),
            autoAck: false,
            consumer: consumer
        );
    }

    private static void SendReplyMessage(string replyMessage, IModel channel, BasicDeliverEventArgs ea)
    {
        var props = ea.BasicProperties;
        var replyProps = channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;

        var responseBytes = Encoding.UTF8.GetBytes(replyMessage);

        channel.BasicPublish(string.Empty, props.ReplyTo, replyProps, responseBytes);
    }

    private static OrderStatus ProccessOrderStatus(decimal amount)
    {
        return OrderServices.OnStore(amount);
    }
}