using Bus.Domain;
using MediatR;
using Report.Domain.Commands;

namespace Report.Domain.CommandHandlers;

public class ReportOcurrenceCommandHandler(IBus bus) : IRequestHandler<ReportOccurenceCommand>
{
    private readonly IBus _bus = bus;

    public Task Handle(ReportOccurenceCommand request, CancellationToken cancellationToken)
    {
        var @event = new ReportOccurenceEvent(
            request.LicensePlate, 
            request.Description, 
            request.Type);
            
        _bus.Publish(@event);
        return Task.FromResult(true);
    }
}
