using CoinBazaar.Infrastructure.MessageQueue;
using CoinBazaar.Infrastructure.Models;
using MediatR;

namespace CoinBazaar.Transfer.Application.Commands
{
    [ConsumerTopic("CreateTransferCommand")]
    public class CreateTransferCommand : IRequest<DomainCommandResponse>, INotification
    {
        public string FromWallet { get; set; }
        public string ToWallet { get; set; }
        public decimal Amount { get; set; }
    }
}
