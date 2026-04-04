using ChatService.Data;
using ChatService.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    public interface IMessageService
    {
        Task<Message> SaveEventAsync(object eventData);
        Task<Message?> GetMessageAsync(long id);
        Task<List<Message>> GetMessagesByTypeAsync(string eventType);
        Task<object?> RecreateEventAsync(long messageId);
    }

    public class MessageService : IMessageService
    {
        private readonly ChatDbContext _context;

        public MessageService(ChatDbContext context)
        {
            _context = context;
        }

        public async Task<Message> SaveEventAsync(object eventData)
        {
            var message = new Message(eventData);
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<Message?> GetMessageAsync(long id)
        {
            return await _context.Messages.FindAsync(id);
        }

        public async Task<List<Message>> GetMessagesByTypeAsync(string eventType)
        {
            return await _context.Messages
                .Where(m => m.Type == eventType)
                .OrderByDescending(m => m.Id)
                .ToListAsync();
        }

        public async Task<object?> RecreateEventAsync(long messageId)
        {
            var message = await GetMessageAsync(messageId);
            return message?.RecreateMessage();
        }
    }
}