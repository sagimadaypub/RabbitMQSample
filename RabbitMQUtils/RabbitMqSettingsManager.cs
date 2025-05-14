using RabbitMQUtils;

namespace RabbitMQUtils
{
    public class RabbitMqSettingsManager
    {
        public Dictionary<string, (string Exchange, string RoutingKey, string RequestQueue, string ResponseQueue)> Config { get; set; } = new Dictionary<string, (string Exchange, string RoutingKey, string RequestQueue, string ResponseQueue)>();
        public RabbitMqSettingsManager()
        {
            Config.Add("user.login", ("user", "user.login", "user.loginRequests", "user.loginResponses"));
            Config.Add("user.registration", ("user", "user.registration", "user.registrationRequests", "user.registrationResponses"));
            Config.Add("user.passwordReset", ("user", "user.passwordReset", "user.passwordResetRequests", "user.passwordResetResponses"));
        }
        public (string exchange, string routingKey, string queueName, string responseQueueName) GetProducerConfig(string key)
        {
            try
            {
                return Config[key];
            }
            catch (Exception)
            {
                throw;
            }        
        }
        public RabbitMqSettings GetProducerSettings(string key)
        {
            return new RabbitMqSettings(GetProducerConfig(key));
        }
    }
}
