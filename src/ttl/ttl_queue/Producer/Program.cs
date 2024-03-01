
using System.Text;
using RabbitMQ.Client;

public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        {
            Uri = new Uri("amqps://ijtvydws:dBbJc7kuCC0ulXc5NW6b0L-iUyphn1nQ@jackal.rmq.cloudamqp.com/ijtvydws")
        };

        using var connection = factory.CreateConnection();
        
        var channel = CreateChannel(connection);

        PublishMessage(channel, Guid.NewGuid().ToString());
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        var args = new Dictionary<string, object>
        {
            { "x-message-ttl", 22000 }
        };

        channel.QueueDeclare("order", false, false, false, args);
        return channel;
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(string.Empty, "order", null, body);
    }
}

