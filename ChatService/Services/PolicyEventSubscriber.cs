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

            // Declare exchange and queues
            _channel.ExchangeDeclare(exchange: "policy.events", type: ExchangeType.Topic, durable: true);
            
            // Queue for PolicyCreated events
            _channel.QueueDeclare(queue: "policy.created.chatservice", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "policy.created.chatservice", exchange: "policy.events", routingKey: "policy.created");

            // Queue for PolicyTerminated events
            _channel.QueueDeclare(queue: "policy.terminated.chatservice", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "policy.terminated.chatservice", exchange: "policy.events", routingKey: "policy.terminated");

            // Queue for ProductActivated events
            _channel.QueueDeclare(queue: "product.activated.chatservice", durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: "product.activated.chatservice", exchange: "policy.events", routingKey: "product.activated");

            // Consumer for PolicyCreated
            var policyCreatedConsumer = new EventingBasicConsumer(_channel);
            policyCreatedConsumer.Received += async (model, ea) =>
            {
                await ProcessMessage<PolicyCreated>(ea, ProcessPolicyCreatedEvent);
            };
            _channel.BasicConsume(queue: "policy.created.chatservice", autoAck: false, consumer: policyCreatedConsumer);

            // Consumer for PolicyTerminated
            var policyTerminatedConsumer = new EventingBasicConsumer(_channel);
            policyTerminatedConsumer.Received += async (model, ea) =>
            {
                await ProcessMessage<PolicyTerminated>(ea, ProcessPolicyTerminatedEvent);
            };
            _channel.BasicConsume(queue: "policy.terminated.chatservice", autoAck: false, consumer: policyTerminatedConsumer);

            // Consumer for ProductActivated
            var productActivatedConsumer = new EventingBasicConsumer(_channel);
            productActivatedConsumer.Received += async (model, ea) =>
            {
                await ProcessMessage<ProductActivated>(ea, ProcessProductActivatedEvent);
            };
            _channel.BasicConsume(queue: "product.activated.chatservice", autoAck: false, consumer: productActivatedConsumer);

            _logger.LogInformation("PolicyEventSubscriber started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start PolicyEventSubscriber");
            throw;
        }
    }

    private async Task ProcessMessage<T>(BasicDeliverEventArgs ea, Func<T, Task> processor)
    {
        try
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var eventData = JsonConvert.DeserializeObject<T>(message);

            if (eventData != null)
            {
                await processor(eventData);
            }

            _channel?.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            _channel?.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private async Task ProcessPolicyCreatedEvent(PolicyCreated msg)
    {
        try
        {
            _logger.LogInformation($"Received PolicyCreated event: {msg.PolicyNumber}");

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

            await SendNotifications(notification);
            _logger.LogInformation($"SignalR notifications sent for policy: {msg.PolicyNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing PolicyCreated event: {msg.PolicyNumber}");
            await SendErrorNotification("Failed to process policy creation notification");
        }
    }

    private async Task ProcessPolicyTerminatedEvent(PolicyTerminated msg)
    {
        try
        {
            _logger.LogInformation($"Received PolicyTerminated event: {msg.PolicyNumber}");

            var notification = new
            {
                Type = "PolicyTerminated",
                Title = "🚫 Policy Terminated",
                Message = $"Policy {msg.PolicyNumber} for {msg.CustomerName} was terminated. Reason: {msg.TerminationReason}",
                PolicyNumber = msg.PolicyNumber,
                CustomerName = msg.CustomerName,
                TerminationReason = msg.TerminationReason,
                FinalPremium = msg.FinalPremium,
                TerminatedBy = msg.TerminatedBy,
                TerminatedAt = msg.TerminatedAt,
                Timestamp = DateTime.UtcNow
            };

            await SendNotifications(notification);
            _logger.LogInformation($"SignalR notifications sent for terminated policy: {msg.PolicyNumber}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing PolicyTerminated event: {msg.PolicyNumber}");
            await SendErrorNotification("Failed to process policy termination notification");
        }
    }

    private async Task ProcessProductActivatedEvent(ProductActivated msg)
    {
        try
        {
            _logger.LogInformation($"Received ProductActivated event: {msg.ProductName} for policy {msg.PolicyNumber}");

            var featuresText = msg.ProductFeatures.Any() 
                ? string.Join(", ", msg.ProductFeatures.Select(kv => $"{kv.Key}: {kv.Value}"))
                : "Standard features";

            var notification = new
            {
                Type = "ProductActivated",
                Title = "✨ Product Activated!",
                Message = $"Product '{msg.ProductName}' ({msg.ProductType}) activated for {msg.CustomerName} on policy {msg.PolicyNumber}",
                ProductId = msg.ProductId,
                ProductName = msg.ProductName,
                ProductType = msg.ProductType,
                PolicyNumber = msg.PolicyNumber,
                CustomerName = msg.CustomerName,
                ProductPremium = msg.ProductPremium,
                ActivatedBy = msg.ActivatedBy,
                ProductFeatures = featuresText,
                ActivatedAt = msg.ActivatedAt,
                Timestamp = DateTime.UtcNow
            };

            await SendNotifications(notification);
            _logger.LogInformation($"SignalR notifications sent for activated product: {msg.ProductName}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing ProductActivated event: {msg.ProductName}");
            await SendErrorNotification("Failed to process product activation notification");
        }
    }

    private async Task SendNotifications(object notification)
    {
        // Gửi tới tất cả clients
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification.GetType().GetProperty("Message")?.GetValue(notification));
        
        // Gửi notification chi tiết tới group PolicyNotifications
        await _hubContext.Clients.Group("PolicyNotifications").SendAsync("ReceivePolicyNotification", notification);

        // Gửi toast notification
        var toastType = notification.GetType().GetProperty("Type")?.GetValue(notification)?.ToString() switch
        {
            "PolicyCreated" => "success",
            "PolicyTerminated" => "warning", 
            "ProductActivated" => "info",
            _ => "info"
        };

        await _hubContext.Clients.All.SendAsync("ReceiveToast", new
        {
            Type = toastType,
            Title = notification.GetType().GetProperty("Title")?.GetValue(notification),
            Message = notification.GetType().GetProperty("Message")?.GetValue(notification),
            Duration = 5000
        });
    }

    private async Task SendErrorNotification(string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveToast", new
        {
            Type = "error",
            Title = "❌ Notification Error",
            Message = message,
            Duration = 3000
        });
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}