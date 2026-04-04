using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    public interface ITransactionalEventService
    {
        Task SaveDataAndPublishEventAsync<T>(Func<ChatDbContext, Task> dataOperation, T eventData);
        Task SaveChatMessageAndPublishEventAsync(ChatMessage chatMessage, object eventData);
    }

    public class TransactionalEventService : ITransactionalEventService
    {
        private readonly ChatDbContext _context;
        private readonly IOutboxService _outboxService;
        private readonly ILogger<TransactionalEventService> _logger;

        public TransactionalEventService(
            ChatDbContext context,
            IOutboxService outboxService,
            ILogger<TransactionalEventService> logger)
        {
            _context = context;
            _outboxService = outboxService;
            _logger = logger;
        }

        public async Task SaveDataAndPublishEventAsync<T>(Func<ChatDbContext, Task> dataOperation, T eventData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Thực hiện thao tác dữ liệu
                await dataOperation(_context);

                // 2. Lưu event vào outbox trong cùng transaction
                await _outboxService.AddToOutboxAsync(eventData);

                // 3. Commit transaction
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully saved data and added event {typeof(T).Name} to outbox");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Failed to save data and add event {typeof(T).Name} to outbox");
                throw;
            }
        }

        public async Task SaveChatMessageAndPublishEventAsync(ChatMessage chatMessage, object eventData)
        {
            await SaveDataAndPublishEventAsync(async (context) =>
            {
                context.ChatMessages.Add(chatMessage);
                await context.SaveChangesAsync();
            }, eventData);
        }
    }
}