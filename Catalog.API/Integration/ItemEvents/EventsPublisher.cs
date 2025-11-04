using Catalog.API.Integration;
using Microsoft.AspNetCore.Connections;
using Microsoft.CodeAnalysis.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NuGet.Protocol.Plugins;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Channels;

namespace Catalog.API.Integration.ItemEvents
{
    public interface IEventsPublisher
    {
        public Task<bool> Publish(Object message);
        public void Start();
        public void Stop();
    }

    public class EventsPublisher : IEventsPublisher, IDisposable
    {
        private readonly RabbitOptions _rabbitOptions;
        private RabbitMQ.Client.IConnection _connection;
        private RabbitMQ.Client.IConnectionFactory _factory;
        private IChannel _channel;
        private readonly string _exchange;
        private readonly string _queuename;
        private readonly string _routingkey;
        private readonly IServiceProvider _services;
        private readonly ILogger<EventsPublisher> _logger;
        private bool _disposed;




        public EventsPublisher(IOptions<RabbitOptions> rabbitOptions, IOptions<RabbitMqExchange> opts_exchange,
            IServiceProvider services, ILogger<EventsPublisher> logger)
        {
            _rabbitOptions = rabbitOptions.Value;
            _logger = logger;
            _services = services;
            _disposed = false;
            _exchange = opts_exchange.Value.Exchange;
            _queuename = opts_exchange.Value.QueueName;
            _routingkey = opts_exchange.Value.RoutingKeys;

        }

        public async Task<bool> TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");
            int i = 0;
            while ((_connection == null) && (i < _rabbitOptions.RetryAttempt))
            {
                try
                {
                    _connection = await _factory.CreateConnectionAsync();
                }
                catch (Exception ex)
                {
                    Thread.Sleep(5000);
                    _logger.LogError("RabbitMQ Client is trying to connect. Retry: " + i);
                }
                i++;
            }

            if (_connection != null)
            {
                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);
                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened.");
                return false;
            }

        }

        public async void Start()
        {
            _factory = new RabbitMQ.Client.ConnectionFactory()
            {

                HostName = _rabbitOptions.HostName,
                Port = _rabbitOptions.Port,
                UserName = _rabbitOptions.UserName,
                Password = _rabbitOptions.Password,
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(15)

            };

            if (await TryConnect())
            {
                _channel = await _connection.CreateChannelAsync();
                await _channel.ExchangeDeclareAsync(exchange: _exchange, type: "direct", durable: true);
                QueueDeclareOk ok = await _channel.QueueDeclareAsync(_queuename, true, false, false, null);
                await _channel.QueueBindAsync(queue: _queuename, exchange: _exchange, routingKey: _routingkey);
                var consumer = new AsyncEventingBasicConsumer(_channel);
                //define the handle to be used when the event consumer is triggered
            }
        }

        public async Task<bool> Publish(Object catalogtEvent)
        {
            if (_connection != null)
            {

                var json = JsonConvert.SerializeObject(catalogtEvent);
                var body = Encoding.UTF8.GetBytes(json);
                await _channel.BasicPublishAsync(_exchange, _routingkey, body);
                return true;
            }
            return false;
        }

        public async void Stop()
        {
            await _channel.CloseAsync(200, "Goodbye");
            await _connection.CloseAsync();
        }
        public bool IsConnected
        {
            get
            {
                return _connection != null && _connection.IsOpen && !_disposed;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }


    }
}