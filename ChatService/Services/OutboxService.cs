using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    public interface IOutboxService
    {
        Task<OutboxMessage> AddToOutboxAsync(object eventData);
        Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 100);
        Task MarkAsProcessedAsync(long outboxMessageId);
        Task MarkAsProcessedAsync(List<long> outboxMessageIds);
        Task<int> GetUnprocessedCountAsync();
    }

    public class OutboxService : IOutboxService
    {
        private readonly ChatDbContext _context;

        public OutboxService(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<OutboxMessage> AddToOutboxAsync(object eventData)
        {
            var outboxMessage = new OutboxMessage(eventData);
            _context.OutboxMessages.Add(outboxMessage);
            await _context.SaveChangesAsync();
            return outboxMessage;
        }

        public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 100)
        {
            return await _context.OutboxMessages
                .Where(m => !m.IsProcessed)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(long outboxMessageId)
        {
            var message = await _context.OutboxMessages.FindAsync(outboxMessageId);
            if (message != null)
            {
                message.MarkAsProcessed();
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsProcessedAsync(List<long> outboxMessageIds)
        {
            var messages = await _context.OutboxMessages
                .Where(m => outboxMessageIds.Contains(m.Id))
                .ToListAsync();

            foreach (var message in messages)
            {
                message.MarkAsProcessed();
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnprocessedCountAsync()
        {
            return await _context.OutboxMessages
                .CountAsync(m => !m.IsProcessed);
        }
    }
}