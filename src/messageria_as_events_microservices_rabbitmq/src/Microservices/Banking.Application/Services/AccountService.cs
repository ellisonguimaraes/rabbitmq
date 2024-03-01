using Banking.Application.Models;
using Banking.Domain.Commands;
using Banking.Domain.Interfaces;
using Banking.Domain.Models;
using MSRabbitMQ.Domain.Core.Bus;

namespace Banking.Application;

public class AccountService(IAccountRepository accountRepository, IEventBus bus) : IAccountService
{
    private readonly IAccountRepository _accountRepository = accountRepository;
    private readonly IEventBus _bus = bus;

    public IEnumerable<Account> GetAccounts()
        => _accountRepository.GetAccounts();

    public void Transfer(AccountTransfer accountTransfer)
    {
        var createTransferCommand = new CreateTransferCommand(accountTransfer.FromAccount, accountTransfer.ToAccount, accountTransfer.TransferAmount);
        _bus.SendCommand(createTransferCommand);
    }
}
