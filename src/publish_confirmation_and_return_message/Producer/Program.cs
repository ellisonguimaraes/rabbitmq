using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

        var channel = CreateChannel(connection);

        var message = Guid.NewGuid().ToString();

        PublishMessage(channel, message);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "", 
            routingKey: "orderr", 
            mandatory: true, 
            basicProperties: null, 
            body: body);

        channel.WaitForConfirms(new TimeSpan(0, 0, 10));
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.ConfirmSelect();

        channel.BasicAcks += Channel_BasicAcks;
        channel.BasicNacks += Channel_BasicNacks;
        channel.BasicReturn += Channel_BasicReturn;

        channel.QueueDeclare("order", false, false, false, null);

        return channel;
    }

    private static void Channel_BasicReturn(object? sender, BasicReturnEventArgs e)
    {
        Console.WriteLine($"[*] Return: {DateTime.Now:d}");
    }

    private static void Channel_BasicNacks(object? sender, BasicNackEventArgs e)
    {
        Console.WriteLine($"[X] Nack: {DateTime.Now:d}");
    }

    private static void Channel_BasicAcks(object? sender, BasicAckEventArgs e)
    {
        Console.WriteLine($"[O] Ack: {DateTime.Now:d}");
    }
}
