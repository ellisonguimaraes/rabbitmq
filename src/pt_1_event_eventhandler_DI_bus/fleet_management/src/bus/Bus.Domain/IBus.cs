using Bus.Domain.Events;

namespace Bus.Domain;

public interface IBus
{
    void Publish<T>(T @event) where T : Event;

    void Subscribe<T, THandler>()
        where T : Event
        where THandler : IEventHandler<T>;
}
