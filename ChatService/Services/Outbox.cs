using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    /// <summary>
    /// Outbox implementation - Concrete class cho outbox operations
    /// </summary>
    public class Outbox : IOutbox
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<Outbox> _logger;

        public Outbox(ChatDbContext context, ILogger<Outbox> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10)
        {
            return await _context.OutboxMessages
                .Where(m => !m.IsProcessed)
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync();
        }

        public async Task MarkAsProcessedAsync(long messageId)
        {
            var message = await _context.OutboxMessages.FindAsync(messageId);
            if (message != null)
            {
                message.MarkAsProcessed();
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAsProcessedAsync(List<long> messageIds)
        {
            var messages = await _context.OutboxMessages
                .Where(m => messageIds.Contains(m.Id))
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