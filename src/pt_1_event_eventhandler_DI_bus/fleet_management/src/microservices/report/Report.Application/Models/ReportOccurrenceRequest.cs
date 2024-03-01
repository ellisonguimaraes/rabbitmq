using Report.Domain.Emums;

namespace Report.Application.Models;

public class ReportOccurrenceRequest
{
    public string LicensePlate { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public OccurrenceType Type { get; set; }
}
