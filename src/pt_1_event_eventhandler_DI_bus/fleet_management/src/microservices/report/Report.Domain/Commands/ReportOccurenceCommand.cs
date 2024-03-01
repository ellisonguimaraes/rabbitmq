using MediatR;
using Report.Domain.Emums;

namespace Report.Domain.Commands;

public class ReportOccurenceCommand : IRequest
{
    public string LicensePlate { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public OccurrenceType Type { get; set; }
}
