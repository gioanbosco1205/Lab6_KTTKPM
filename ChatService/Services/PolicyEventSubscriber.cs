using ChatService.Events;
using ChatService.Hubs;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;

namespace ChatService.Services;

public class PolicyEventSubscriber
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly IHubContext<AgentChatHub> _hubContext;
    private readonly ILogger<PolicyEventSubscriber> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public PolicyEventSubscriber(
        IConnectionFactory connectionFactory, 
        IHubContext<AgentChatHub> hubContext,
        ILogger<PolicyEventSubscriber> logger)
    {
        _connectionFactory = connectionFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting PolicyEventSubscriber...");

        try
        {
            _connection = _connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange and queue
            _channel.ExchangeDeclare(exchange: "policy.events", type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: "policy.created.chatservice", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "policy.created.chatservice", exchange: "policy.events", routingKey: "policy.created");

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var policyCreated = JsonConvert.DeserializeObject<PolicyCreated>(message);

                    if (policyCreated != null)
                    {
                        await ProcessPolicyCreatedEvent(policyCreated);
                    }

                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                }
            };

            _channel.BasicConsume(queue: "policy.created.chatservice", autoAck: false, consumer: consumer);
            _logger.LogInformation("PolicyEventSubscriber started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start PolicyEventSubscriber");
            throw;
        }
    }

    private async Task ProcessPolicyCreatedEvent(PolicyCreated msg)
    {
        try
        {
            _logger.LogInformation($"Received PolicyCreated event: {msg.PolicyNumber}");

            // Tạo notification object với nhiều thông tin hơn
            var notification = new
            {
                Type = "PolicyCreated",
                Title = "🎉 New Policy Created!",
                Message = $"Policy {msg.PolicyNumber} was created with premium ${msg.Premium:F2}",
                PolicyNumber = msg.PolicyNumber,
                Premium = msg.Premium,
                Status = msg.Status,
                CreatedAt = msg.CreatedAt,
                Timestamp = DateTime.UtcNow
            };

            // ⭐ GỬI NOTIFICATION QUA SIGNALR
            // Gửi tới tất cả clients
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification.Message);
            
            // Gửi notification chi tiết tới group PolicyNotifications
            await _hubContext.Clients.Group("PolicyNotifications").SendAsync("ReceivePolicyNotification", notification);

            // Gửi toast notification
            await _hubContext.Clients.All.SendAsync("ReceiveToast", new
            {
                Type = "success",
                Title = notification.Title,
                Message = notification.Message,
                Duration = 5000
            });

            _logger.LogInformation($"SignalR notifications sent for policy: {msg.PolicyNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing PolicyCreated event: {msg.PolicyNumber}");
            
            // Gửi error notification
            await _hubContext.Clients.All.SendAsync("ReceiveToast", new
            {
                Type = "error",
                Title = "❌ Notification Error",
                Message = "Failed to process policy notification",
                Duration = 3000
            });
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}