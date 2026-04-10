using ChatService.Services;

namespace ChatService.Services
{
    /// <summary>
    /// SimpleOutboxSendingService - Theo đúng pattern trong yêu cầu
    /// ASP.NET Core cung cấp IHostedService để chạy background job
    /// Service này chạy mỗi 1 second
    /// </summary>
    public class SimpleOutboxSendingService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SimpleOutboxSendingService> _logger;
        private Timer? _timer;

        public SimpleOutboxSendingService(
            IServiceProvider serviceProvider,
            ILogger<SimpleOutboxSendingService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SimpleOutboxSendingService started");

            // Timer chạy mỗi 1 giây như trong yêu cầu
            _timer = new Timer(
                PushMessages,
                null,
                TimeSpan.Zero,                    // Start immediately
                TimeSpan.FromSeconds(1)           // Run every 1 second
            );

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SimpleOutboxSendingService is stopping");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        /// <summary>
        /// PushMessages - Được gọi mỗi 1 giây bởi Timer
        /// </summary>
        private async void PushMessages(object? state)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
                var rabbitPublisher = scope.ServiceProvider.GetRequiredService<IRabbitEventPublisher>();

                // Lấy messages chưa xử lý
                var messages = await outboxService.GetUnprocessedMessagesAsync(5);

                foreach (var message in messages)
                {
                    try
                    {
                        var eventData = message.RecreateEvent();
                        if (eventData != null)
                        {
                            await rabbitPublisher.PublishAsync(eventData);
                            await outboxService.MarkAsProcessedAsync(message.Id);
                            
                            _logger.LogDebug($"Pushed message {message.Id} of type {message.Type}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to push message {message.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PushMessages");
            }
        }
    }
}