using ChatService.Events;
using ChatService.Hubs;
using Microsoft.AspNetCore.SignalR;
using RawRabbit;

namespace ChatService.Services;

public class PolicyEventSubscriber
{
    private readonly IBusClient _busClient;
    private readonly IHubContext<AgentChatHub> _hubContext;
    private readonly ILogger<PolicyEventSubscriber> _logger;

    public PolicyEventSubscriber(
        IBusClient busClient, 
        IHubContext<AgentChatHub> hubContext,
        ILogger<PolicyEventSubscriber> logger)
    {
        _busClient = busClient;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task StartAsync()
    {
        _logger.LogInformation("Starting PolicyEventSubscriber...");

        // ⭐ PHẦN 14 - SIGNALR NOTIFICATION
        await _busClient.SubscribeAsync<PolicyCreated>(async msg =>
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
        });

        _logger.LogInformation("PolicyEventSubscriber started successfully");
    }
}