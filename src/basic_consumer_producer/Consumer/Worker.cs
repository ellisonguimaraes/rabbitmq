using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            UserName = "ellison",
            Password = "Abcd@1234",
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare("order", false, false, false, null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += Consumer_Received;

        channel.BasicConsume("order", true, consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation($"Worker active: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await Task.Delay(10000, stoppingToken);
        }
    }

    private void Consumer_Received(object? sender, BasicDeliverEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.Body.Span);
        Console.WriteLine($"Received message: {message}");
    }
}
