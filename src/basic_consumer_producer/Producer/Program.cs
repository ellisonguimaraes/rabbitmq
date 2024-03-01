using System.Text;
using RabbitMQ.Client;

var counter = 0;

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

while (true)
{   
    var message = $"Message number {counter}.";
    var body = Encoding.UTF8.GetBytes(message);
    channel.BasicPublish(string.Empty, "order", null, body);

    Console.WriteLine($"Mensagem enviada: {message}");
    Console.WriteLine("Press [ENTER] to send message...");
    Console.ReadLine();

    counter++;
}