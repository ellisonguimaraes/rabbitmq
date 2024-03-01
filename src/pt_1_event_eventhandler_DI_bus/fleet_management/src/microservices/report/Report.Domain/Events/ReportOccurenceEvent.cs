using Bus.Domain.Events;
using Report.Domain.Emums;

namespace Report.Domain;

public class ReportOccurenceEvent(
    string licensePlate, 
    string description, 
    OccurrenceType type) : Event
{
    public string LicensePlate { get; set; } = licensePlate;
    public string Description { get; set; } = description;
    public OccurrenceType Type { get; set; } = type;
}
