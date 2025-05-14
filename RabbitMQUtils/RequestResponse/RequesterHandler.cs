using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQUtils.RequestResponse
{
    //to-do:
    //set key for settings
    //set the receive event
    public class RequesterHandler : IRequesterResponderHandler
    {
        private readonly RabbitMqConnectionManager _connectionManager;
        private readonly RabbitMqSettingsManager _settingsManager;
        public readonly RabbitMqSettings _settings;

        //public delegate Task<string> OnReceiveJob (string corId, string message);
        public string RequesterResult { get; private set; } = string.Empty;
        public bool ResponseStatus { get; set; } = false;
        private bool InitializeStatus { get; set; } = false;
        public TimeSpan? Timeout { get; set; }

        public TaskCompletionSource<string>? AwaitConsume { get; private set; }
        public event IRequesterResponderHandler.OnReceiveJob? OnReceiveEvent;


        public RequesterHandler(RabbitMqConnectionManager connectionManager, RabbitMqSettingsManager settingsManager, RabbitMqSettings settings, IRequesterResponderHandler.OnReceiveJob? onReceiveJob = null)
        {
            _connectionManager = connectionManager;
            _settingsManager = settingsManager;
            _settings = settings;
            if (onReceiveJob != null)
                OnReceiveEvent += onReceiveJob;
        }

        //make sure our exchange, Queue and Bind exist before using them
        //this is needed in case rabbitmq is not persist or this is the first time or the first user of it
        // Setup rabbitmq to fit this request-response process
        public async Task InitializeAsync()
        {
            try
            {
                // Validate connection and channel for the settings
                var connection = await _connectionManager.GetConnectionAsync();
                await using var channel = await connection.CreateChannelAsync();

                // Ensure exchange exists
                await channel.ExchangeDeclareAsync(_settings.exchangeName, ExchangeType.Direct, durable: true);

                // Ensure queue exists
                var requestQResults = await channel.QueueDeclareAsync(queue: _settings.queueName, durable: true, exclusive: false, autoDelete: false);

                // Ensure queue exists
                var responseQResults = await channel.QueueDeclareAsync(queue: _settings.responseQueueName, durable: true, exclusive: false, autoDelete: false);

                if (requestQResults == null || responseQResults == null)
                {
                    InitializeStatus = false;
                    return;
                }

                // Ensure queue is bound to the exchange
                await channel.QueueBindAsync(_settings.queueName, _settings.exchangeName, _settings.queueName);
                // Ensure queue is bound to the exchange
                await channel.QueueBindAsync(_settings.responseQueueName, _settings.exchangeName, _settings.responseQueueName);
                InitializeStatus = true;
            }
            catch (Exception e)
            {
                InitializeStatus = false;
                Console.WriteLine($"RabbitMQ initialization failed: {e.Message}");
                throw;
            }
        }

        public async Task EnsureInitializedAsync()
        {
            if (!InitializeStatus)
            {
                await InitializeAsync();
            }
        }

        public async Task<bool> RequestAndWait(string message)
        {
            //make sure queues ready to use
            await EnsureInitializedAsync();
            if(!InitializeStatus) return false;
            //set up the message id and the channel to response to
            //this is how the resqueser will identify what response with
            //and the responder will use to response for the dedicated queue
            string correlationId = Guid.NewGuid().ToString();
            string result = string.Empty;
            ResponseStatus = false;
            
            var basicProp = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = _settings.responseQueueName
            };

            //ensure connection ok
            var conenction = await _connectionManager.GetConnectionAsync();
            //create channel to rabbit for handle request and response
            using (var channel = await conenction.CreateChannelAsync())
            {
                //initiate consumer
                var consumer = new AsyncEventingBasicConsumer(channel);
                AwaitConsume = new TaskCompletionSource<string>();

                consumer.ReceivedAsync += async (m, e) =>
                {
                    if (e.BasicProperties.CorrelationId == correlationId)
                    {
                        var body = e.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        if (AwaitConsume.TrySetResult(message))
                        {
                            await channel.BasicAckAsync(e.DeliveryTag, multiple: false);
                        }
                    }
                };

                //convert the message to bytes for publish
                var messageBody = Encoding.UTF8.GetBytes(message);
                //publish the message
                await channel.BasicPublishAsync(_settings.exchangeName, _settings.queueName,
                    true, basicProp, messageBody);

                var consumerTag = await channel.BasicConsumeAsync(_settings.responseQueueName, false, consumer);

                ResponseStatus = await WaitForResponse(AwaitConsume);
                await channel.BasicCancelAsync(consumerTag);
            }

            return ResponseStatus;
        }

        private async Task<bool> WaitForResponse(TaskCompletionSource<string> tcs, TimeSpan? timeout = null)
        {
            var timeoutTask = Task.Delay(timeout ?? new TimeSpan(0, 0, 30));
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                tcs.TrySetException(new TimeoutException());
                return false;
            }
            else
            {
                RequesterResult = await tcs.Task;
                return true;
            }
        }
    }
}
