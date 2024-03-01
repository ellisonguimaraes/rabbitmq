using Bus.Domain.Events;
using Ocurrences.Domain.Core.Entities;
using Ocurrences.Domain.Events;
using Ocurrences.Infra.Data.Interfaces;

namespace Ocurrences.Domain.EventHandlers;

public class ReportOccurenceEventHandler(IOcurrenceRepository repository) : IEventHandler<ReportOccurenceEvent>
{
    private readonly IOcurrenceRepository _repository = repository;
    
    public async Task Handle(ReportOccurenceEvent @event)
    {
        Console.WriteLine("Reported occurence vehicle: " + @event.LicensePlate);

        var ocurrence = new Ocurrence {
            Description = @event.Description,
            LicensePlate = @event.LicensePlate,
            Type = @event.Type
        };

        await _repository.CreateAsync(ocurrence);
    }
}
