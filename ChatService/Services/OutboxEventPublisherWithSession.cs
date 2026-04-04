using ChatService.Models;

namespace ChatService.Services
{
    /// <summary>
    /// OutboxEventPublisher sử dụng ISession pattern
    /// Thay vì gửi RabbitMQ trực tiếp, lưu event vào database
    /// </summary>
    public class OutboxEventPublisherWithSession : IEventPublisher
    {
        private readonly ISession _session;
        private readonly ILogger<OutboxEventPublisherWithSession> _logger;

        public OutboxEventPublisherWithSession(ISession session, ILogger<OutboxEventPublisherWithSession> logger)
        {
            _session = session;
            _logger = logger;
        }

        public async Task PublishMessage<T>(T message)
        {
            try
            {
                // Lưu event vào database thay vì gửi RabbitMQ trực tiếp
                // Event trở thành data trong database
                await _session.SaveAsync(new Message(message));
                
                _logger.LogInformation($"Event {typeof(T).Name} saved to database as Message entity");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish event {typeof(T).Name} to database");
                throw;
            }
        }
    }
}