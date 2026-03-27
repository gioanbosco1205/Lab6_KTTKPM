using RabbitMQ.Client;
using System.Text;
using Newtonsoft.Json;

namespace ChatService.Services;

public class RabbitEventPublisher
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitEventPublisher> _logger;

    public RabbitEventPublisher(IConnectionFactory connectionFactory, ILogger<RabbitEventPublisher> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishMessage<T>(T message)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            using var channel = connection.CreateModel();

            var exchangeName = "policy.events";
            var routingKey = typeof(T).Name.ToLower();

            channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic, durable: true);

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);

            channel.BasicPublish(exchange: exchangeName, routingKey: routingKey, basicProperties: null, body: body);
            
            _logger.LogInformation($"Published message of type {typeof(T).Name} to exchange {exchangeName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to publish message of type {typeof(T).Name}");
            throw;
        }
    }
}