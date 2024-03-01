
using System.Text;
using RabbitMQ.Client;

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

        var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "order", 
            durable: true, 
            exclusive: false, 
            autoDelete: false, 
            arguments: null);

        var message = Guid.NewGuid().ToString();
        var body = Encoding.UTF8.GetBytes(message);

        var props = channel.CreateBasicProperties();

        props.Persistent = true;

        channel.BasicPublish(
            exchange: string.Empty, 
            routingKey: "order", 
            basicProperties: props, 
            body: body);        
    }
}
