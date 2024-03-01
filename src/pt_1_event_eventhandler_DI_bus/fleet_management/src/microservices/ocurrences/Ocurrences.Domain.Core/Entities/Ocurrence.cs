using Ocurrences.Domain.Core.Enums;

namespace Ocurrences.Domain.Core.Entities;

public class Ocurrence
{
    public int Id { get; set; }
    
    public string LicensePlate { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public OccurrenceType Type { get; set; }
}
