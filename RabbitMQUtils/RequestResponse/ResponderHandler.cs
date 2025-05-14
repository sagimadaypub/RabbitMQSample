using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQUtils.RequestResponse
{
    ///this class is a reponser to rabbitmq requests in queues
    /// <summary>
    /// this class start a consumer that wait for a desegnated queue
    /// when the consumer get read a message he process it
    /// after finishing process it, it will publish a response to a response queue
    /// the messages will be marked by a correlationId
    /// the response queue will be determined by replyTo property
    /// if no replyTo property the response queue will publish the response to the response queue in the responser settings
    /// ***Ideal to use in a worker project, 
    /// </summary>
    public class ResponderHandler : IDisposable, IRequesterResponderHandler
    {
        private readonly RabbitMqConnectionManager _connectionManager;
        private readonly RabbitMqSettingsManager _settingsManager;
        public readonly RabbitMqSettings _settings;

        //public delegate Task<string> OnReceiveJob(string corId, string message);
        private IChannel? channel { get; set; }
        private AsyncEventingBasicConsumer? consumer { get; set; }

        //public string RequesterResult { get; private set; } = string.Empty;
        public bool ResponseStatus { get; set; } = false;
        private bool InitializeStatus { get; set; }

        //public TaskCompletionSource<string>? AwaitConsume { get; private set; }
        public event IRequesterResponderHandler.OnReceiveJob? OnReceiveEvent;

        public ResponderHandler(RabbitMqConnectionManager connectionManager, RabbitMqSettingsManager settingsManager, RabbitMqSettings settings, IRequesterResponderHandler.OnReceiveJob? onReceiveJob = null)
        {
            _connectionManager = connectionManager;
            _settingsManager = settingsManager;
            _settings = settings;
            if (onReceiveJob != null)
                OnReceiveEvent += onReceiveJob;
        }

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

        public async Task<bool> ListenAndRespond()
        {
            try
            {
                //make sure rabbitmq queues available
                await EnsureInitializedAsync();
                if (!InitializeStatus) return false;
                //ensure connection ok
                var conenction = await _connectionManager.GetConnectionAsync();
                //create channel to rabbit for handle request and response
                if (channel != null)
                    channel.Dispose();
                channel = channel ?? await conenction.CreateChannelAsync();
                //create consumer, release prev consumer if exist
                if (consumer != null && consumer.ConsumerTags.FirstOrDefault() != null)
                    await channel.BasicCancelAsync(consumer.ConsumerTags.First());
                consumer = new AsyncEventingBasicConsumer(channel);
                //set consumer receive event
                consumer.ReceivedAsync += async (m, e) =>
                {
                    //get the message body
                    var body = e.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    //get correlationid for reference
                    var correlationId = e.BasicProperties.CorrelationId;
                    //check if everything needed to consume is vailable
                    if (!string.IsNullOrEmpty(correlationId) && !string.IsNullOrEmpty(message) && OnReceiveEvent is not null)
                    {
                        //do event for processing the message
                        string result = string.Empty;
                        result = await OnReceiveEvent.Invoke(correlationId, message);
                        //create properties for response
                        var basicProp = new BasicProperties
                        {
                            CorrelationId = e.BasicProperties.CorrelationId,
                        };
                        //set message ready to publish
                        var resBody = Encoding.UTF8.GetBytes(result);
                        //publish the message to the response queue
                        await channel.BasicPublishAsync(_settings.exchangeName, _settings.responseQueueName, true, basicProp, resBody);
                        //ack the message to remove from consumed queue
                        await channel.BasicAckAsync(e.DeliveryTag, multiple: false);
                    }
                };
                await channel.BasicConsumeAsync(_settings.queueName, false, consumer);
                ResponseStatus = true;
            }
            catch (Exception e)
            {
                ResponseStatus = false;
            }
            return ResponseStatus;
        }

        public async void Dispose()
        {
            if(channel != null)
            {
                if (consumer != null && consumer.ConsumerTags.FirstOrDefault() != null)
                    await channel.BasicCancelAsync(consumer.ConsumerTags.First());
                channel.Dispose();
            }
            if (OnReceiveEvent is not null)
            {
                foreach (var d in OnReceiveEvent.GetInvocationList())
                {
                    OnReceiveEvent -= (IRequesterResponderHandler.OnReceiveJob)d;
                }
            }
        }
    }
}
