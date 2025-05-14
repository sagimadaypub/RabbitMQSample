using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQUtils
{
    public class RabbitMqSettings
    {
        public string exchangeName { get; set; } =string.Empty;
        public string queueName { get; set; } = string.Empty;
        public string routingKey { get; set; } = string.Empty;
        public string responseQueueName { get; set; } = string.Empty;
        public RabbitMqSettings()
        {
            
        }

        public RabbitMqSettings((string exchangeName, string queueName, string routingKey, string responseQueueName) tupleSettings)
        {
            exchangeName = tupleSettings.exchangeName;
            queueName = tupleSettings.queueName;
            routingKey = tupleSettings.routingKey;
            responseQueueName = tupleSettings.responseQueueName;
        }
    }
}
