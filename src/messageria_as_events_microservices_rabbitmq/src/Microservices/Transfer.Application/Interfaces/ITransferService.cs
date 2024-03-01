using Transfer.Domain;

namespace Transfer.Application;

public interface ITransferService
{
    IEnumerable<TransferLog> GetTransferLogs();
}
