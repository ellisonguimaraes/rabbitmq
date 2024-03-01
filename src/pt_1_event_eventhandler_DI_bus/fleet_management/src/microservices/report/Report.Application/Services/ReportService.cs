using MediatR;
using Report.Application.Interfaces;
using Report.Application.Models;
using Report.Domain.Commands;

namespace Report.Application;

public class ReportService(IMediator mediator) : IReportService
{
    private readonly IMediator _mediator = mediator;

    public void ReportOcurrence(ReportOccurrenceRequest report)
    {
        var command = new ReportOccurenceCommand { LicensePlate = report.LicensePlate, Description = report.Description, Type = report.Type };
        _mediator.Send(command);
    }
}
