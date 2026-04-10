using ChatService.Models;

namespace ChatService.Services
{
    /// <summary>
    /// IOutbox interface - Abstraction cho outbox operations
    /// </summary>
    public interface IOutbox
    {
        Task<List<OutboxMessage>> ReadMessagesAsync(int batchSize = 10);
        Task PublishMessageAsync(OutboxMessage message);
        Task DeleteMessageAsync(long messageId);
        
        // ⭐ Retry mechanism methods
        Task IncrementRetryCountAsync(long messageId);
        Task MoveToDeadLetterQueueAsync(long messageId);
        
        // Legacy methods
        Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10);
        Task MarkAsProcessedAsync(long messageId);
        Task MarkAsProcessedAsync(List<long> messageIds);
        Task<int> GetUnprocessedCountAsync();
    }
}