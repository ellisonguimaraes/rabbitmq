using Banking.Domain.Interfaces;
using Banking.Domain.Models;

namespace Banking.Infra.Data;

public class AccountRepository(BankingDbContext context) : IAccountRepository
{
    private readonly BankingDbContext _context = context;

    public IEnumerable<Account> GetAccounts()
        => _context.Accounts;
}
