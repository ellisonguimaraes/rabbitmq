using MSRabbitMQ.Domain.Core.Bus;
using Transfer.Domain;

namespace Transfer.Application;

public class TransferService(ITransferRepository transferRepository, IEventBus bus) : ITransferService
{
    private readonly ITransferRepository _transferRepository = transferRepository;
    private readonly IEventBus _bus = bus;

    public IEnumerable<TransferLog> GetTransferLogs()
    {
        return _transferRepository.GetTransferLogs();
    }
}
