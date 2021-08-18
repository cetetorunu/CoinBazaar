using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue
{
    public interface IConnection:IDisposable
    {
        IChannel CreateChannel();
    }
}
