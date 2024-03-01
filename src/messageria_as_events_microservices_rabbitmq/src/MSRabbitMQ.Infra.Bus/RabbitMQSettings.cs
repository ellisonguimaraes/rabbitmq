namespace MSRabbitMQ.Infra.Bus;

public class RabbitMQSettings
{
    public string HostName { get; set; } = string.Empty;

    public int Port { get; set; } = default;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
