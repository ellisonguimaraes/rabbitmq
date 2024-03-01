using MSRabbitMQ.Domain.Core.Events;

namespace MSRabbitMQ.Domain.Core.Commands;

public abstract class Command : Message
{
    public DateTime Timestamp { get; protected set; }

    public Command()
    {
        Timestamp = DateTime.Now;
    }
}
