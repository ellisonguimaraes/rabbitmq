using Bus.Domain.Events;
using Ocurrences.Domain.Core.Enums;

namespace Ocurrences.Domain.Events;

public class ReportOccurenceEvent(string licensePlate, string description, OccurrenceType type) : Event
{
    public string LicensePlate { get; set; } = licensePlate;

    public string Description { get; set; } = description;

    public OccurrenceType Type { get; set; } = type;
}

