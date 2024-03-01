using System.Text;
using RabbitMQ.Client;

public class Program 
{   
    public static void Main(string[] args)
    {
        var counter = 0;
        var exchange = "order_topic";

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "ellison",
            Password = "Abcd@1234",
            Port = 5672
        };

        using var connection = factory.CreateConnection();

        var channel = SetupChannel(connection, exchange);

        while(true)
        {
            foreach (var rk in args)
                for (int i = 0; i < 10; i++)
                    PublishMessage(channel, exchange, rk, $"{counter++}");
            
            Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
            Console.ReadLine();
        }
    }

    static void PublishMessage(IModel channel, string exchange, string routingKey, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        // Publish in queue
        channel.BasicPublish(exchange, routingKey, null, body);
    }

    static IModel SetupChannel(IConnection connection, string exchange)
    {
        // Create channel
        var channel = connection.CreateModel();

        // Define queue
        channel.QueueDeclare("finance", false, false, false, null);
        channel.QueueDeclare("finance_ba", false, false, false, null);
        channel.QueueDeclare("finance_ba_lg", false, false, false, null);
        channel.QueueDeclare("finance_ba_rh", false, false, false, null);
        channel.QueueDeclare("finance_rj", false, false, false, null);
        channel.QueueDeclare("finance_rj_lg", false, false, false, null);
        channel.QueueDeclare("finance_rj_rh", false, false, false, null);
        channel.QueueDeclare("finance_rh", false, false, false, null);

        // Define exchange
        channel.ExchangeDeclare("order_topic", "topic");

        // Bind queue
        channel.QueueBind("finance", exchange, "finance");
        channel.QueueBind("finance", exchange, "finance.#");
        channel.QueueBind("finance_ba", exchange, "finance.ba");
        channel.QueueBind("finance_ba", exchange, "finance.ba.*");
        channel.QueueBind("finance_ba_lg", exchange, "finance.ba.lg");
        channel.QueueBind("finance_ba_rh", exchange, "finance.ba.rh");
        channel.QueueBind("finance_rj", exchange, "finance.rj");
        channel.QueueBind("finance_rj", exchange, "finance.rj.*");
        channel.QueueBind("finance_rj_lg", exchange, "finance.rj.lg");
        channel.QueueBind("finance_rj_rh", exchange, "finance.rj.rh");
        channel.QueueBind("finance_rh", exchange, "finance.*.rh");

        return channel;
    }
}