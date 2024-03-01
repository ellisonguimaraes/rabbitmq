using Transfer.Domain;

namespace Transfer.Infra.Data;

public class TransferRepository(TransferDbContext context) : ITransferRepository
{
    private readonly TransferDbContext _context = context;

    public void AddTransferLog(TransferLog log)
    {
        _context.Add(log);
        _context.SaveChanges();
    }

    public IEnumerable<TransferLog> GetTransferLogs()
    {
        return _context.TransferLogs;
    }
}
