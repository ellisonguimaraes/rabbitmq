using MSRabbitMQ.Domain.Core.Bus;

namespace Transfer.Domain;

public class TransferEventHandler(ITransferRepository transferRepository) : IEventHandler<TransferCreatedEvent>
{
    private readonly ITransferRepository _transferRepository = transferRepository;

    public Task Handle(TransferCreatedEvent @event)
    {
        var transaction = new TransferLog
        {
           FromAccount = @event.From,
           ToAccount = @event.To,
           TransferAmount = @event.Amount
        };

        _transferRepository.AddTransferLog(transaction);

        return Task.CompletedTask;
    }
}
