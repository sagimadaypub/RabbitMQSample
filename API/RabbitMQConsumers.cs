using RabbitMQUtils;
using RabbitMQUtils.RequestResponse;

namespace API
{
    public class RabbitMQConsumers : BackgroundService
    {
        private readonly RabbitMqConnectionManager _connectionManager;
        private readonly RabbitMqSettingsManager _settingsManager;
        public ResponderHandler? loginConsumer { get; set; }
        public ResponderHandler? registrationConsumer { get; set; }

        public bool StartParallel { get; set; } = false;

        public RabbitMQConsumers(RabbitMqConnectionManager connectionManager, RabbitMqSettingsManager settingsManager)
        {
            _connectionManager = connectionManager;
            _settingsManager = settingsManager;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            loginConsumer = new ReqResBuilder()
                .WithConnection(_connectionManager)
                .WithSettings(_settingsManager, "user.login")
                .WithTimeout(new TimeSpan(0, 0, 10))
                .WithEvent(async (corId, message) =>
                {
                    Console.WriteLine($"loginConsumer consumed with corId:'{corId}' and message:'{message}'.");
                    return $"loginConsumer consumed with corId:'{corId}' and message:'{message}'.";
                }).CreateResponder();
            registrationConsumer = new ReqResBuilder()
                .WithConnection(_connectionManager)
                .WithSettings(_settingsManager, "user.registration")
                .WithTimeout(new TimeSpan(0, 0, 10))
                .WithEvent(async (corId, message) =>
                {
                    Console.WriteLine($"registrationConsumer consumed with corId:'{corId}' and message:'{message}'.");
                    return $"registrationConsumer consumed with corId:'{corId}' and message:'{message}'.";
                }).CreateResponder();

            if(StartParallel)
            {
                var t1 = loginConsumer.ListenAndRespond();
                var t2 = registrationConsumer.ListenAndRespond();

                // Optional: log if they fail (don't block here)
                _ = Task.WhenAll(t1, t2).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        Console.WriteLine("One or more consumers failed to start:");
                        Console.WriteLine(t.Exception);
                    }
                });
            }
            else
            {
                await loginConsumer.ListenAndRespond();
                await registrationConsumer.ListenAndRespond();
            }

            while (!stoppingToken.IsCancellationRequested) { }
        }
    }
}
