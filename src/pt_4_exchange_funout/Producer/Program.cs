using System.Text;
using RabbitMQ.Client;

var exchange = "order_fanout";
var counter = 0;

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

using var connection = factory.CreateConnection();

var channel = SetupChannel(connection, exchange, "order", "logs", "finance");

while(true)
{
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, exchange, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

static void PublishMessage(IModel channel, string exchange, string message)
{
    var body = Encoding.UTF8.GetBytes(message);

    // Publish in exchange
    channel.BasicPublish(exchange, string.Empty, null, body);
}

static IModel SetupChannel(IConnection connection, string enchange, params string[] queues)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    foreach(var queue in queues)
        channel.QueueDeclare(queue, false, false, false, null);

    // Define exchange
    channel.ExchangeDeclare(enchange, "fanout");

    // Bind queue
    foreach(var queue in queues)
        channel.QueueBind(queue, enchange, string.Empty);

    return channel;
}