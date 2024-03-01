using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Program
{
    public static void Main(string[] args)
    {
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

        // Cria channel
        var channel = CreateChannel(connection, args);

        // Cria consumers
        foreach (var queue in args)
            CreateConsumer(channel, queue);

        Console.WriteLine("Press [ENTER] to exit consumer...");
        Console.ReadLine();
    }

    static IModel CreateChannel(IConnection connection, params string[] queues)
    {
        // Create channel
        var channel = connection.CreateModel();

        // Define prefetch
        channel.BasicQos(0, 2, false);

        // Define queue
        foreach (var queue in queues)
            channel.QueueDeclare(queue, false, false, false, null);

        return channel;
    }

    static void CreateConsumer(IModel channel, string queueName)
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
                Console.WriteLine($"[x] Received message (queue: {queueName}): {message}");

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
}