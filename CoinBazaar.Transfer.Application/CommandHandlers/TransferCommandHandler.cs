using CoinBazaar.Infrastructure.EventBus;
using CoinBazaar.Infrastructure.MessageQueue;
using CoinBazaar.Infrastructure.Models;
using CoinBazaar.Transfer.Application.Commands;
using CoinBazaar.Transfer.Domain;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoinBazaar.Transfer.Application.CommandHandlers
{
    public class TransferCommandHandler : IRequestHandler<CreateTransferCommand, DomainCommandResponse>
    {
        //private readonly IEventRepository _eventRepository;
        private readonly IChannel _channel;
        public TransferCommandHandler(/*IEventRepository eventRepository,*/ IChannel channel)
        {
            //_eventRepository = eventRepository;
            _channel = channel;
        }

        public async Task<DomainCommandResponse> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
        {
            var aggregateRoot = new TransferAggregateRoot(Guid.NewGuid(), request.FromWallet, request.ToWallet, request.Amount);

            //await _eventRepository.SaveAsync(aggregateRoot).ConfigureAwait(false);

            await _channel.Publish(new CreateTransferCommand() { Amount = 10, FromWallet = "Gökhan" });


            //return await _eventRepository.Publish(domainEventResult);
            return new DomainCommandResponse { AggregateId = aggregateRoot.AggregateId, CreationDate = DateTime.UtcNow };
        }
    }
}
