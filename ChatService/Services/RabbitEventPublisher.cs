using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace ChatService.Services;

public interface IRabbitEventPublisher
{
    Task PublishAsync<T>(T message);
}

public class RabbitEventPublisher : IRabbitEventPublisher, IDisposable
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

    public async Task PublishAsync<T>(T message)
    {
        try
        {
            EnsureConnection();

            var exchangeName = "policy.events";
            var routingKey = typeof(T).Name.ToLower();

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            _channel!.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: null, body: body);
            
            _logger.LogInformation($"Published message of type {typeof(T).Name} to exchange {exchangeName}");
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

    // Backward compatibility
    public async Task PublishMessage<T>(T message)
    {
        await PublishAsync(message);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}