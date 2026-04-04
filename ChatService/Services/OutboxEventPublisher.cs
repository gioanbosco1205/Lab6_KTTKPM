using ChatService.Models;

namespace ChatService.Services
{
    /// <summary>
    /// OutboxEventPublisher - Implementation của Outbox Pattern
    /// Thay vì gửi RabbitMQ trực tiếp, lưu event vào database
    /// Events sẽ được xử lý bởi background service sau đó
    /// </summary>
    public class OutboxEventPublisher : IEventPublisher
    {
        private readonly ISession _session;
        private readonly ILogger<OutboxEventPublisher> _logger;

        public OutboxEventPublisher(ISession session, ILogger<OutboxEventPublisher> logger)
        {
            _session = session;
            _logger = logger;
        }

        public async Task PublishMessage<T>(T message)
        {
            try
            {
                // Lưu event vào Message table (Event Store)
                await _session.SaveAsync(new Message(message));

                // Lưu event vào OutboxMessage table để background service xử lý
                await _session.SaveAsync(new OutboxMessage(message));

                _logger.LogInformation($"Event {typeof(T).Name} saved to database successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to publish event {typeof(T).Name} to database");
                throw;
            }
        }
    }
}