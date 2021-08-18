using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue.Camunda
{
    public class CamundaConnection : IConnection
    {
        private readonly IMediator _mediator;

        public CamundaConnection(IMediator mediator)
        {
            this._mediator = mediator;
        }
        public IChannel CreateChannel()
        {
            return new CamundaChannel("http://localhost:8080/engine-rest", _mediator);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
