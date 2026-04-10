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
        /// ⭐ Với retry mechanism (max retry = 5) và comprehensive logging
        /// </summary>
        private async void PushMessages(object? state)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IOutbox>();

                // 1. Read messages
                var messages = await outbox.ReadMessagesAsync(5);

                if (messages.Count > 0)
                {
                    _logger.LogInformation("═══════════════════════════════════════════════════════");
                    _logger.LogInformation($"[Outbox] Starting batch processing: {messages.Count} messages");
                    _logger.LogInformation("═══════════════════════════════════════════════════════");
                }

                var successCount = 0;
                var failureCount = 0;
                var retryCount = 0;
                var deadLetterCount = 0;

                foreach (var message in messages)
                {
                    var messageStartTime = DateTime.UtcNow;
                    
                    try
                    {
                        _logger.LogInformation($"[Outbox] Processing message {message.Id} (Type: {message.Type})");
                        
                        // 2. Publish message
                        await outbox.PublishMessageAsync(message);
                        
                        // 3. Delete message
                        await outbox.DeleteMessageAsync(message.Id);
                        
                        var processingTime = (DateTime.UtcNow - messageStartTime).TotalMilliseconds;
                        
                        // ✅ SUCCESS LOG
                        _logger.LogInformation("┌─────────────────────────────────────────────────────┐");
                        _logger.LogInformation("│ ✅ SUCCESS                                          │");
                        _logger.LogInformation("├─────────────────────────────────────────────────────┤");
                        _logger.LogInformation($"│ Message ID: {message.Id,-38} │");
                        _logger.LogInformation($"│ Type: {message.Type,-44} │");
                        _logger.LogInformation($"│ Processing Time: {processingTime:F2}ms{new string(' ', 30)} │");
                        _logger.LogInformation($"│ Status: Published & Deleted{new string(' ', 24)} │");
                        _logger.LogInformation("└─────────────────────────────────────────────────────┘");
                        
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        var processingTime = (DateTime.UtcNow - messageStartTime).TotalMilliseconds;
                        
                        // ❌ FAILURE LOG
                        _logger.LogError("┌─────────────────────────────────────────────────────┐");
                        _logger.LogError("│ ❌ FAILURE                                          │");
                        _logger.LogError("├─────────────────────────────────────────────────────┤");
                        _logger.LogError($"│ Message ID: {message.Id,-38} │");
                        _logger.LogError($"│ Type: {message.Type,-44} │");
                        _logger.LogError($"│ Error: {ex.Message,-43} │");
                        _logger.LogError($"│ Processing Time: {processingTime:F2}ms{new string(' ', 30)} │");
                        _logger.LogError("└─────────────────────────────────────────────────────┘");
                        
                        failureCount++;
                        
                        // Increment retry count
                        try
                        {
                            await outbox.IncrementRetryCountAsync(message.Id);
                            
                            // Check if exceeded max retries
                            var context = scope.ServiceProvider.GetRequiredService<ChatService.Data.ChatDbContext>();
                            var dbMessage = await context.Messages.FindAsync(message.Id);
                            
                            if (dbMessage != null)
                            {
                                if (dbMessage.HasExceededMaxRetries(5))
                                {
                                    // ⚠️ DEAD LETTER LOG
                                    _logger.LogCritical("┌─────────────────────────────────────────────────────┐");
                                    _logger.LogCritical("│ ⚠️  DEAD LETTER QUEUE                               │");
                                    _logger.LogCritical("├─────────────────────────────────────────────────────┤");
                                    _logger.LogCritical($"│ Message ID: {message.Id,-38} │");
                                    _logger.LogCritical($"│ Type: {message.Type,-44} │");
                                    _logger.LogCritical($"│ Retry Count: {dbMessage.RetryCount,-35} │");
                                    _logger.LogCritical($"│ Last Retry: {dbMessage.LastRetryAt,-36} │");
                                    _logger.LogCritical($"│ Status: Max retries exceeded (5){new string(' ', 19)} │");
                                    _logger.LogCritical("└─────────────────────────────────────────────────────┘");
                                    
                                    await outbox.MoveToDeadLetterQueueAsync(message.Id);
                                    deadLetterCount++;
                                }
                                else
                                {
                                    // 🔄 RETRY LOG
                                    _logger.LogWarning("┌─────────────────────────────────────────────────────┐");
                                    _logger.LogWarning("│ 🔄 RETRY SCHEDULED                                  │");
                                    _logger.LogWarning("├─────────────────────────────────────────────────────┤");
                                    _logger.LogWarning($"│ Message ID: {message.Id,-38} │");
                                    _logger.LogWarning($"│ Type: {message.Type,-44} │");
                                    _logger.LogWarning($"│ Retry Count: {dbMessage.RetryCount}/5{new string(' ', 32)} │");
                                    _logger.LogWarning($"│ Last Retry: {dbMessage.LastRetryAt,-36} │");
                                    _logger.LogWarning($"│ Next Retry: In 1 second{new string(' ', 28)} │");
                                    _logger.LogWarning("└─────────────────────────────────────────────────────┘");
                                    
                                    retryCount++;
                                }
                            }
                        }
                        catch (Exception retryEx)
                        {
                            _logger.LogError(retryEx, $"[Outbox] Failed to handle retry for message {message.Id}");
                        }
                    }
                }

                // BATCH SUMMARY
                if (messages.Count > 0)
                {
                    var totalTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                    
                    _logger.LogInformation("═══════════════════════════════════════════════════════");
                    _logger.LogInformation("│ 📊 BATCH SUMMARY                                    │");
                    _logger.LogInformation("├─────────────────────────────────────────────────────┤");
                    _logger.LogInformation($"│ Total Messages: {messages.Count,-34} │");
                    _logger.LogInformation($"│ ✅ Success: {successCount,-38} │");
                    _logger.LogInformation($"│ ❌ Failures: {failureCount,-37} │");
                    _logger.LogInformation($"│ 🔄 Retries: {retryCount,-38} │");
                    _logger.LogInformation($"│ ⚠️  Dead Letter: {deadLetterCount,-34} │");
                    _logger.LogInformation($"│ Total Time: {totalTime:F2}ms{new string(' ', 33)} │");
                    _logger.LogInformation("═══════════════════════════════════════════════════════");
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "❌ CRITICAL ERROR in OutboxSendingService.PushMessages");
            }
        }
    }
}