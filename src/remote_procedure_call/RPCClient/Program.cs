using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RPCClient.Domain;

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

        var correlationId = Guid.NewGuid().ToString();

        InitializerConsumer(channel, correlationId);

        var props = channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = $"{nameof(Order)}_return";

        while (true)
        {
            Console.Write("Valor do pedido: ");

            var amount = decimal.Parse(Console.ReadLine()!);

            var order = new Order(amount);
            var message = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(string.Empty, nameof(Order), props, body);

            Console.WriteLine($"Published: {message}");
            Console.ReadLine();
            Console.Clear();
        }
    }

    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: $"{nameof(Order)}_return",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        channel.QueueDeclare(
            queue: nameof(Order),
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        return channel;
    }

    private static void InitializerConsumer(IModel channel, string correlationId)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) => 
        {
            if (correlationId.Equals(ea.BasicProperties.CorrelationId))
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received: {message}");

                return;
            }

            Console.WriteLine($"Mensagem descartada, identificadores de correlacão inválidos. Original: {correlationId}, Recebido: {ea.BasicProperties.CorrelationId}");
        };

        channel.BasicConsume(
            queue: $"{nameof(Order)}_return",
            autoAck: true,
            consumer: consumer
        );
    }
}