using MediatR;
using Microsoft.AspNetCore.Mvc;
using Report.Domain.Commands;
using Report.Domain.Emums;

namespace Report.API;

[ApiController]
[Route("api/[controller]")]
public class ReportController(IMediator mediator) : ControllerBase
{
    private readonly IMediator _mediator = mediator;

    [HttpPost]
    public IActionResult Post(
        [FromHeader] string licensePlate,
        [FromHeader] string description,
        [FromHeader] OccurrenceType type)
    {
        var command = new ReportOccurenceCommand
        {
            LicensePlate = licensePlate,
            Description = description,
            Type = type
        };

        _mediator.Send(command);

        return NoContent();
    }
}
