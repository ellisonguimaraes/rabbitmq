using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory() {
            Uri = new Uri("amqps://ijtvydws:dBbJc7kuCC0ulXc5NW6b0L-iUyphn1nQ@jackal.rmq.cloudamqp.com/ijtvydws")
        };

        using var connection = factory.CreateConnection();
        var channel = SetupChannel(connection);

        CreateConsumer(channel);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }

    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.ExchangeDeclare("DeadLetterExchange", ExchangeType.Fanout);
        channel.QueueDeclare("DeadLetterQueue", false, false, false, null);
        channel.QueueBind("DeadLetterQueue", "DeadLetterExchange", string.Empty);

        var arguments = new Dictionary<string, object>() { {"x-dead-letter-exchange", "DeadLetterExchange"} };

        channel.QueueDeclare("order", false, false, false, arguments); 
        channel.BasicQos(0, 1, false);

        return channel;
    }

    private static void CreateConsumer(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (sender, ea) => {
            try 
            {
                var body = ea.Body.Span;
                var message = Encoding.UTF8.GetString(body);

                var number = int.Parse(message);

                Console.WriteLine($"[Success]\t{number}");

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Failed]\t{ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, );
            }
        };

        channel.BasicConsume("order", false, consumer);
    }
}