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
            foreach (var header in args)
                for (int i = 0; i < 10; i++)
                    PublishMessage(channel, exchange, header, $"{counter++}");
            
            Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
            Console.ReadLine();
        }
    }

    static void PublishMessage(IModel channel, string exchange, string headerValue, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        // Publish in exchange with header
        var headers = new Dictionary<string, object> { {"section", headerValue} };

        var basicProperties = channel.CreateBasicProperties();
        basicProperties.Headers = headers;

        channel.BasicPublish(exchange, string.Empty, basicProperties, body);
    }

    static IModel SetupChannel(IConnection connection, string exchange)
    {
        // Create channel
        var channel = connection.CreateModel();

        // Define queue
        channel.QueueDeclare("logistics", false, false, false, null);
        channel.QueueDeclare("accounting", false, false, false, null);
        channel.QueueDeclare("finance", false, false, false, null);
        channel.QueueDeclare("auditorship", false, false, false, null);

        // Define exchange
        channel.ExchangeDeclare(exchange, "headers");

        // Bind queue
        var financeHeaders = new Dictionary<string, object> { {"section", "finance"} };
        channel.QueueBind("finance", exchange, string.Empty, financeHeaders);
        
        var accountingHeaders = new Dictionary<string, object> { {"section", "accounting"} };
        channel.QueueBind("accounting", exchange, string.Empty, accountingHeaders);
        
        var logisticsHeaders = new Dictionary<string, object> { {"section", "logistics"} };
        channel.QueueBind("logistics", exchange, string.Empty, logisticsHeaders);

        var auditorshipHeaders = new Dictionary<string, object> { {"section", "finance"}};
        channel.QueueBind("auditorship", exchange, string.Empty, auditorshipHeaders);

        return channel;
    }
}