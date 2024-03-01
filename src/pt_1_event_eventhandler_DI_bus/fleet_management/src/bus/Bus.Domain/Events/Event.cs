namespace Bus.Domain.Events;

public abstract class Event
{
    public DateTime Timestamp { get; protected set; } = DateTime.UtcNow;
}
