namespace Transfer.Domain;

public interface ITransferRepository
{
    IEnumerable<TransferLog> GetTransferLogs();

    void AddTransferLog(TransferLog log);
}
