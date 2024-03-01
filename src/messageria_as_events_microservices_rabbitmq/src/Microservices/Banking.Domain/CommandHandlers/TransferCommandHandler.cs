using Banking.Domain.Commands;
using Banking.Domain.Events;
using MediatR;
using MSRabbitMQ.Domain.Core.Bus;

namespace Banking.Domain.CommandHandlers;

public class TransferCommandHandler(IEventBus bus) : IRequestHandler<CreateTransferCommand, bool>
{
    private readonly IEventBus _bus = bus;

    public Task<bool> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        var transferCreatedEvent = new TransferCreatedEvent(request.From, request.To, request.Amount);

        _bus.Publish(transferCreatedEvent);

        return Task.FromResult(true);
    }
}
