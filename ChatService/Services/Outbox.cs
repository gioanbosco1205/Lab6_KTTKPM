using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    /// <summary>
    /// Outbox implementation - Concrete class cho outbox operations
    /// PHẦN 6 - OUTBOX PROCESSOR
    /// Class Outbox chịu trách nhiệm: read message, publish message, delete message
    /// Xử lý Message table (Event Store) theo đúng yêu cầu
    /// 
    /// PHẦN 7 - Registered as Singleton
    /// Sử dụng IServiceProvider để tạo scope mới cho mỗi operation (vì DbContext là Scoped)
    /// </summary>
    public class Outbox : IOutbox
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRabbitEventPublisher _rabbitPublisher;
        private readonly ILogger<Outbox> _logger;

        public Outbox(
            IServiceProvider serviceProvider,
            IRabbitEventPublisher rabbitPublisher,
            ILogger<Outbox> logger)
        {
            _serviceProvider = serviceProvider;
            _rabbitPublisher = rabbitPublisher;
            _logger = logger;
        }

        public async Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10)
        {
           
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();

            var messages = await context.Messages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)  // ⭐ Max retry = 5
                .OrderBy(m => m.Id)
                .Take(batchSize)
                .ToListAsync();

            // Convert sang OutboxMessage format (giữ nguyên ID để delete sau)
            var result = new List<OutboxMessage>();
            foreach (var msg in messages)
            {
                if (msg.Id.HasValue)
                {
                    // Sử dụng internal constructor để tạo OutboxMessage từ Message
                    result.Add(new OutboxMessage(msg.Id.Value, msg.Type, msg.Payload));
                }
            }
            
            _logger.LogDebug($"[Outbox] Read {result.Count} unprocessed messages (retry count < 5) from Messages table");
            return result;
        }

        public async Task PublishMessageAsync(OutboxMessage message)
        {
            var eventData = message.RecreateEvent();
            if (eventData != null)
            {
                // PUBLISH to RabbitMQ exchange
                await _rabbitPublisher.PublishAsync(eventData);
                _logger.LogInformation($"[Outbox] Published message {message.Id} of type {message.Type}");
            }
            else
            {
                _logger.LogWarning($"[Outbox] Could not recreate event from message {message.Id}");
            }
        }

      
        public async Task DeleteMessageAsync(long messageId)
        {
            // Tạo scope mới để lấy DbContext
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            // DELETE từ Message table (Event Store)
            // Sử dụng ExecuteDeleteAsync - tương đương với ExecuteUpdate() trong NHibernate
            await context.Messages
                .Where(m => m.Id == messageId)
                .ExecuteDeleteAsync();
            
            _logger.LogInformation($"[Outbox] Deleted message {messageId} from Messages table");
        }

        public async Task IncrementRetryCountAsync(long messageId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            var message = await context.Messages.FindAsync(messageId);
            if (message != null)
            {
                message.IncrementRetryCount();
                await context.SaveChangesAsync();
                
                _logger.LogWarning($"[Outbox] Incremented retry count for message {messageId}. RetryCount: {message.RetryCount}, LastRetryAt: {message.LastRetryAt}");
            }
        }

      
        public async Task MoveToDeadLetterQueueAsync(long messageId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            var message = await context.Messages.FindAsync(messageId);
            if (message != null)
            {
                // Mark as processed để không retry nữa
                message.MarkAsProcessed();
                await context.SaveChangesAsync();
                
                _logger.LogError($"[Outbox] Message {messageId} moved to dead letter queue after {message.RetryCount} retries. Type: {message.Type}");
            }
        }

        // ========== Legacy methods for OutboxMessage table (backward compatibility) ==========
        
        public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            return await context.OutboxMessages
                .Where(m => !m.IsProcessed)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(long messageId)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            var message = await context.OutboxMessages.FindAsync(messageId);
            if (message != null)
            {
                message.MarkAsProcessed();
                await context.SaveChangesAsync();
            }
        }

        public async Task MarkAsProcessedAsync(List<long> messageIds)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            var messages = await context.OutboxMessages
                .Where(m => messageIds.Contains(m.Id))
                .ToListAsync();

            foreach (var message in messages)
            {
                message.MarkAsProcessed();
            }

            await context.SaveChangesAsync();
        }

        public async Task<int> GetUnprocessedCountAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            
            return await context.OutboxMessages
                .CountAsync(m => !m.IsProcessed);
        }
    }
}
