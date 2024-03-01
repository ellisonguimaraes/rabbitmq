using System.Text;
using RabbitMQ.Client;

var queueName = "order";
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

var channel = CreateChannel(connection, queueName);

while(true)
{
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, queueName, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

static void PublishMessage(IModel channel, string queueName, string message)
{
    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    var body = Encoding.UTF8.GetBytes(message);

    // Publish in queue
    channel.BasicPublish(string.Empty, queueName, null, body);
}

static IModel CreateChannel(IConnection connection, string queueName)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    return channel;
}