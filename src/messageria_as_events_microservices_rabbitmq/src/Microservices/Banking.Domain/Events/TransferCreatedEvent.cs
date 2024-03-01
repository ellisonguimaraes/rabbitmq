using MSRabbitMQ.Domain.Core.Events;

namespace Banking.Domain.Events;

public class TransferCreatedEvent(int from, int to, decimal amount) : Event
{
    public int From { get; set; } = from;

    public int To { get; set; } = to;

    public decimal Amount { get; set; } = amount;
}

