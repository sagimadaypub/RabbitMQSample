using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Exceptions;

namespace RabbitMQUtils
{

    public class RabbitMqConnectionManager : IDisposable
    {
        private readonly ConnectionFactory _factory;
        private IConnection? _connection;
        private readonly object _lock = new();

        public RabbitMqConnectionManager(string hostName, int port, string userName, string password)
        {
            _factory = new ConnectionFactory
            {
                HostName = hostName,
                UserName = userName,
                Password = password,
                Port = port == 0 ? 5672 : port,
                AutomaticRecoveryEnabled = true,  // Auto-reconnect enabled
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
            };

            // Try to establish connection with retry logic
            ConnectWithRetry();
        }

        private void ConnectWithRetry()
        {
            int retries = 5;
            int delay = 2000; // 2 seconds

            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
                    Console.WriteLine("RabbitMQ connection established.");
                    return;
                }
                catch (BrokerUnreachableException)
                {
                    Console.WriteLine($"Connection failed. Retrying in {delay / 1000} seconds...");
                    Thread.Sleep(delay);
                }
            }
            throw new Exception("RabbitMQ connection failed after multiple retries.");
        }

        public async Task<IConnection> GetConnectionAsync()
        {
            return await Task.FromResult(_connection ?? throw new Exception("RabbitMQ connection is not available."));
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
