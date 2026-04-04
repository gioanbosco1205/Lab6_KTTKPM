using ChatService.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatService.Services
{
    public class DatabaseSession : ISession
    {
        private readonly ChatDbContext _context;
        private readonly ILogger<DatabaseSession> _logger;

        public DatabaseSession(ChatDbContext context, ILogger<DatabaseSession> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SaveAsync<T>(T entity) where T : class
        {
            try
            {
                _context.Set<T>().Add(entity);
                await _context.SaveChangesAsync();
                _logger.LogDebug($"Saved entity of type {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to save entity of type {typeof(T).Name}");
                throw;
            }
        }

        public async Task<T?> GetAsync<T>(object id) where T : class
        {
            try
            {
                return await _context.Set<T>().FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get entity of type {typeof(T).Name} with id {id}");
                throw;
            }
        }

        public async Task DeleteAsync<T>(T entity) where T : class
        {
            try
            {
                _context.Set<T>().Remove(entity);
                await _context.SaveChangesAsync();
                _logger.LogDebug($"Deleted entity of type {typeof(T).Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete entity of type {typeof(T).Name}");
                throw;
            }
        }

        public async Task FlushAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogDebug("Flushed all pending changes to database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to flush changes to database");
                throw;
            }
        }
    }
}