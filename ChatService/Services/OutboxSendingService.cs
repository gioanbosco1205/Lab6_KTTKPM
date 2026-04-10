using ChatService.Services;

namespace ChatService.Services
{
    /// <summary>
    /// OutboxSendingService - Theo đúng pattern trong yêu cầu
    /// ASP.NET Core cung cấp IHostedService để chạy background job
    /// Service này chạy mỗi 1 second
    /// </summary>
    public class OutboxSendingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxSendingService> _logger;
        private Timer? _timer;

        public OutboxSendingService(
            IServiceProvider serviceProvider,
            ILogger<OutboxSendingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboxSendingService started - running every 1 second");

            // Theo đúng pattern trong yêu cầu
            _timer = new Timer(
                PushMessages,
                null,
                TimeSpan.Zero,                    // Start immediately
                TimeSpan.FromSeconds(1)           // Service này chạy mỗi 1 second
            );

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("OutboxSendingService is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// PushMessages - Method được gọi mỗi 1 giây
        /// Xử lý outbox messages và gửi qua RabbitMQ
        /// </summary>
        private async void PushMessages(object? state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                // Sử dụng IOutbox interface như trong pattern PHẦN 6
                // Class Outbox chịu trách nhiệm: read message, publish message, delete message
                var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

                // 1. Read messages (Đọc tin nhắn)
                var messages = await outbox.ReadMessagesAsync(5);

                if (messages.Count > 0)
                {
                    _logger.LogDebug($"Processing {messages.Count} outbox messages");
                }

                foreach (var message in messages)
                {
                    try
                    {
                        // 2. Publish message (Gửi tin nhắn)
                        await outbox.PublishMessageAsync(message);
                        
                        // 3. Delete message (Xóa tin nhắn sau khi gửi thành công)
                        await outbox.DeleteMessageAsync(message.Id);
                        
                        _logger.LogDebug($"Successfully processed outbox message {message.Id}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to process outbox message {message.Id}");
                        // Không delete để retry lần sau
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in OutboxSendingService.PushMessages");
            }
        }
    }
}