using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQUtils.RequestResponse
{
    public interface IRequesterResponderHandler
    {
        public delegate Task<string> OnReceiveJob(string corId, string message);

    }
}
