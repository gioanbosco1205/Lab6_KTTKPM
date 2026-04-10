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
        
        // Cần thiết cho các service có sẵn hiện tại
        Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10);
        Task MarkAsProcessedAsync(long messageId);
        Task MarkAsProcessedAsync(List<long> messageIds);
        Task<int> GetUnprocessedCountAsync();
    }
}