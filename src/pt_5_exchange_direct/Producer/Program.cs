using System.Text;
using RabbitMQ.Client;

var counter = 0;

var exchange = "order_direct";

const string ORDER_QUEUE = "order";
const string FINANCE_ORDER_QUEUE = "finance_order";
const string ORDER_NEW_RK = "order_new";
const string ORDER_UPD_RK = "order_upd";

// Define um factory connection: informações para conexão com o servidor RabbitMQ
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
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, exchange, ORDER_NEW_RK, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

void PublishMessage(IModel channel, string exchange, string routingKey, string message)
{
    var body = Encoding.UTF8.GetBytes(message);

    // Publish in exchange (with routing key)
    channel.BasicPublish(exchange, routingKey, null, body);
}

IModel SetupChannel(IConnection connection, string exchange)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    channel.QueueDeclare(ORDER_QUEUE, false, false, false, null);
    channel.QueueDeclare(FINANCE_ORDER_QUEUE, false, false, false, null);

    // Define exchange
    channel.ExchangeDeclare(exchange, "direct");

    // Bind queue
    channel.QueueBind(ORDER_QUEUE, exchange, ORDER_NEW_RK);
    channel.QueueBind(ORDER_QUEUE, exchange, ORDER_UPD_RK);
    channel.QueueBind(FINANCE_ORDER_QUEUE, exchange, ORDER_NEW_RK);

    return channel;
}