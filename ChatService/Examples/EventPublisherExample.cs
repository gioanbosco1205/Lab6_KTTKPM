using ChatService.Services;
using ChatService.Events;

namespace ChatService.Examples
{
    /// <summary>
    /// Ví dụ minh họa cách sử dụng OutboxEventPublisher
    /// </summary>
    public class EventPublisherExample
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<EventPublisherExample> _logger;

        public EventPublisherExample(IEventPublisher eventPublisher, ILogger<EventPublisherExample> logger)
        {
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Trước đây: Gửi RabbitMQ trực tiếp
        /// await eventPublisher.PublishMessage(event);
        /// 
        /// Bây giờ: Lưu event vào database
        /// Event trở thành data trong database
        /// </summary>
        public async Task PublishPolicyCreatedEvent()
        {
            var policyCreatedEvent = new PolicyCreated
            {
                PolicyNumber = "POL-2024-001",
                Premium = 1000000,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            // Thay vì gửi RabbitMQ trực tiếp, ta lưu event vào database
            // OutboxEventPublisher sẽ lưu vào cả Message table và OutboxMessage table
            await _eventPublisher.PublishMessage(policyCreatedEvent);

            _logger.LogInformation($"PolicyCreated event for policy {policyCreatedEvent.PolicyNumber} saved to database");
        }

        /// <summary>
        /// Ví dụ publish nhiều events trong một transaction
        /// </summary>
        public async Task PublishMultipleEvents()
        {
            // Event 1: Policy được tạo
            var policyCreated = new PolicyCreated
            {
                PolicyNumber = "POL-2024-001",
                Premium = 1000000,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };

            // Event 2: Policy được kích hoạt
            var policyActivated = new ProductActivated
            {
                PolicyId = "POL-001",
                ProductId = "PROD-001",
                ActivatedAt = DateTime.UtcNow
            };

            // Lưu cả 2 events vào database
            // Background service sẽ xử lý publish chúng sau
            await _eventPublisher.PublishMessage(policyCreated);
            await _eventPublisher.PublishMessage(policyActivated);

            _logger.LogInformation("Multiple events saved to database successfully");
        }
    }
}