using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue
{
    public interface IChannel:IDisposable
    {
        Dictionary<string, Type> Topics { get; set; }
        Task BindAllConsumers();

        Task StartListen();

        Task StopListen();

        Task Publish(INotification @event);
    }
}
