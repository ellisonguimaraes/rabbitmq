using Banking.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Banking.Infra.Data;

public class BankingDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Account> Accounts { get; set; }
}
