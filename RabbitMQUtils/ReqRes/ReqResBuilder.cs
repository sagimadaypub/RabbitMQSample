using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQUtils.ReqRes
{
    public class ReqResBuilder
    {
        private RabbitMqConnectionManager? _connectionManager;
        private RabbitMqSettingsManager? _settingsManager;
        private RabbitMqSettings? _settings;
        private IRequesterResponderHandler.OnReceiveJob? _onReceiveJob;

        private TimeSpan? Timeout;

        public RequesterHandler CreateRequester()
        {
            var r = new RequesterHandler(_connectionManager!, _settingsManager!, _settings!, _onReceiveJob);
            r.Timeout = Timeout;
            return r;
        }
        public ResponderHandler CreateResponder()
        {
            var r = new ResponderHandler(_connectionManager!, _settingsManager!, _settings!, _onReceiveJob);
            return r;
        }

        public ReqResBuilder WithConnection(RabbitMqConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
            return this;
        }

        public ReqResBuilder WithSettings(RabbitMqSettingsManager settingsManager, string key)
        {
            _settingsManager = settingsManager;
            try
            {
                _settings = _settingsManager.GetProducerSettings(key);
            }
            catch (Exception)
            {
                throw;
            }
            return this;
        }

        public ReqResBuilder WithEvent(IRequesterResponderHandler.OnReceiveJob onReceiveJob)
        {
            _onReceiveJob = onReceiveJob;
            return this;
        }

        public ReqResBuilder WithTimeout(TimeSpan? timeout)
        {
            Timeout = timeout;
            return this;
        }
    }
}
