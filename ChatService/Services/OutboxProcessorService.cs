using ChatService.Services;

namespace ChatService.Services
{
    public class OutboxProcessorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxProcessorService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(30); // Xử lý mỗi 30 giây

        public OutboxProcessorService(
            IServiceProvider serviceProvider,
            ILogger<OutboxProcessorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Outbox Processor Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing outbox messages");
                }

                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("Outbox Processor Service stopped");
        }

        private async Task ProcessOutboxMessagesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<IRabbitEventPublisher>();

            var unprocessedMessages = await outboxService.GetUnprocessedMessagesAsync(50);

            if (unprocessedMessages.Count == 0)
            {
                return;
            }

            _logger.LogInformation($"Processing {unprocessedMessages.Count} outbox messages");

            var processedIds = new List<long>();

            foreach (var message in unprocessedMessages)
            {
                try
                {
                    var eventData = message.RecreateEvent();
                    if (eventData != null)
                    {
                        await eventPublisher.PublishAsync(eventData);
                        processedIds.Add(message.Id);
                        _logger.LogDebug($"Published outbox message {message.Id} of type {message.Type}");
                    }
                    else
                    {
                        _logger.LogWarning($"Could not recreate event from outbox message {message.Id}");
                        processedIds.Add(message.Id); // Mark as processed to avoid infinite retry
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to publish outbox message {message.Id}");
                    // Không thêm vào processedIds để retry lần sau
                }
            }

            if (processedIds.Count > 0)
            {
                await outboxService.MarkAsProcessedAsync(processedIds);
                _logger.LogInformation($"Marked {processedIds.Count} outbox messages as processed");
            }
        }
    }
}