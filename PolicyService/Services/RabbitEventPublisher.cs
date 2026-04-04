using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using PolicyService.Events;
using Polly;
using Polly.Retry;

namespace PolicyService.Services;

public class RabbitEventPublisher : IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private readonly RetryPolicy _retryPolicy;
    private readonly object _lock = new();

    public RabbitEventPublisher(IConnectionFactory connectionFactory, ILogger<RabbitEventPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning($"Retry {retryCount} failed to connect to RabbitMQ. Waiting {timeSpan} before next retry. Exception: {exception.Message}");
                });
    }

    private void EnsureConnection()
    {
        if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
        {
            return;
        }

        lock (_lock)
        {
            if (_connection != null && _connection.IsOpen && _channel != null && _channel.IsOpen)
            {
                return;
            }

            _retryPolicy.Execute(() =>
            {
                try
                {
                    _channel?.Dispose();
                    _connection?.Dispose();
                }
                catch { /* Ignore disposal errors */ }

                _connection = _connectionFactory.CreateConnection();
                _channel = _connection.CreateModel();

                var exchangeName = "policy.events";
                _channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

                _logger.LogInformation("Connected to RabbitMQ successfully.");
            });
        }
    }

    public async Task PublishMessage<T>(T message)
    {
        try
        {
            EnsureConnection();

            var exchangeName = "policy.events";
            var routingKey = GetRoutingKey<T>();

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            // Re-run basic publish if it fails immediately (though rare if connection is open)
            _channel!.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: null, body: body);
            
            _logger.LogInformation($"Published {typeof(T).Name} event with routing key '{routingKey}': {json}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish message of type {typeof(T).Name}");
            // Reset connection to force reconnection on next attempt
            _connection = null;
            _channel = null;
            throw;
        }
    }

    private string GetRoutingKey<T>()
    {
        return typeof(T).Name switch
        {
            nameof(PolicyCreated) => "policy.created",
            nameof(PolicyTerminated) => "policy.terminated", 
            nameof(ProductActivated) => "product.activated",
            _ => "policy.unknown"
        };
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}