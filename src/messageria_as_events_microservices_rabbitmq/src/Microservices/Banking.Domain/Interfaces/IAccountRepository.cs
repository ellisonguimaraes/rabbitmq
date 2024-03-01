using Banking.Domain.Models;

namespace Banking.Domain.Interfaces;

public interface IAccountRepository
{
    IEnumerable<Account> GetAccounts();
}
