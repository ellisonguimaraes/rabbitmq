using Microsoft.EntityFrameworkCore;
using Transfer.Domain;

namespace Transfer.Infra.Data;

public class TransferDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<TransferLog> TransferLogs { get; set; }
}
