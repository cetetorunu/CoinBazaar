using MediatR;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue.Rabbit
{
    public class RabbitMQConnection : IConnection, IDisposable
    {
        private readonly IMediator _mediator;

        private RabbitMQ.Client.IConnection _connection;

        public RabbitMQConnection(IMediator mediator)
        {
            this._mediator = mediator;
        }
        public IChannel CreateChannel()
        {
            var uri = new Uri("amqp://guest:guest@rabbit:5672/CUSTOM_HOST");
            var _connectionFactory = new ConnectionFactory
            {
                Uri = uri
            };

            _connection = _connectionFactory.CreateConnection();

            return new RabbitMQChannel(_connection, _mediator);

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
