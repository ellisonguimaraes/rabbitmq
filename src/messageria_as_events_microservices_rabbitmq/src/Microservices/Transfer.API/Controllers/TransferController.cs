using Microsoft.AspNetCore.Mvc;
using Transfer.Application;
using Transfer.Domain;

namespace Transfer.API;

[ApiController]
[Route("api/[controller]")]
public class TransferController(ITransferService transferService) : ControllerBase
{
    private readonly ITransferService _transferService = transferService;

    [HttpGet]
    public ActionResult<IEnumerable<TransferLog>> Get()
    {
        return Ok(_transferService.GetTransferLogs());
    }
}
