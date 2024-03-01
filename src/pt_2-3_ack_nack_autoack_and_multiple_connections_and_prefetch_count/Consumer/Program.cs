using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var queueName = "order";

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

// Cria a conexão
using var connection = factory.CreateConnection();

for(int i = 0; i < 2; i++)
{
    var channel = CreateChannel(connection, queueName);

    // Cria consumers
    for(int j = 0; j < 2; j++)
        CreateConsumer(channel, queueName, i, j);
}

Console.WriteLine("Press [ENTER] to exit consumer...");
Console.ReadLine();

static IModel CreateChannel(IConnection connection, string queueName)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define prefetch
    channel.BasicQos(0, 2, false);

    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    return channel;
}

static void CreateConsumer(IModel channel, string queueName, int channelId, int consumerId)
{
    // Cria um consumer básico
    var consumer = new EventingBasicConsumer(channel);

    // Define o método que será executado quando receber um item da fila
    consumer.Received += (model, ea) =>
    {
        try
        {
            var body = ea.Body.Span;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[x] Received message (channel {channelId}, consumer {consumerId}): {message}");

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception)
        {
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    // Inicia o consumer
    channel.BasicConsume(queueName, false, consumer);
}