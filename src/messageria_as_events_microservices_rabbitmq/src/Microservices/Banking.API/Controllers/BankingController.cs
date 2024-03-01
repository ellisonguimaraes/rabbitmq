using Banking.Application;
using Banking.Application.Models;
using Banking.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Banking.API;

[ApiController]
[Route("api/[controller]")]
public class BankingController(IAccountService accountService) : ControllerBase
{
    private readonly IAccountService _accountService = accountService;

    [HttpGet]
    public ActionResult<IEnumerable<Account>> Get()
        => Ok(_accountService.GetAccounts());

    [HttpPost]
    public IActionResult Post([FromBody] AccountTransfer accountTransfer)
    {
        _accountService.Transfer(accountTransfer);
        return Ok(accountTransfer);
    }
}
