using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue
{
    public class ConsumerHostedService : IHostedService
    {
        private readonly IConnection _connection;
        private IChannel _channel;

        public ConsumerHostedService(IConnection connection)
        {
            this._connection = connection;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _connection.CreateChannel();

            await _channel.BindAllConsumers();
            await _channel.StartListen();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _channel.StopListen();
        }
    }
}
