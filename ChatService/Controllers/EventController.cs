using Microsoft.AspNetCore.Mvc;
using ChatService.Services;
using ChatService.Events;

namespace ChatService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventController : ControllerBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly IOutboxService _outboxService;
        private readonly ILogger<EventController> _logger;

        public EventController(
            IEventPublisher eventPublisher,
            IOutboxService outboxService,
            ILogger<EventController> logger)
        {
            _eventPublisher = eventPublisher;
            _outboxService = outboxService;
            _logger = logger;
        }

        /// <summary>
        /// Demo publish event sử dụng Outbox Pattern
        /// Thay vì gửi RabbitMQ trực tiếp, lưu event vào database
        /// </summary>
        [HttpPost("publish-policy-created")]
        public async Task<IActionResult> PublishPolicyCreated([FromBody] PolicyCreatedRequest request)
        {
            try
            {
                var policyCreatedEvent = new PolicyCreated
                {
                    PolicyNumber = request.PolicyNumber,
                    Premium = request.PremiumAmount,
                    Status = "Created",
                    CreatedAt = DateTime.UtcNow
                };

                // Lưu event vào database thay vì gửi RabbitMQ trực tiếp
                await _eventPublisher.PublishMessage(policyCreatedEvent);

                return Ok(new { 
                    Message = "Event saved to database successfully", 
                    EventType = nameof(PolicyCreated),
                    PolicyNumber = request.PolicyNumber 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PolicyCreated event");
                return StatusCode(500, new { Error = "Failed to publish event" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái outbox
        /// </summary>
        [HttpGet("outbox/status")]
        public async Task<IActionResult> GetOutboxStatus()
        {
            try
            {
                var unprocessedCount = await _outboxService.GetUnprocessedCountAsync();
                var unprocessedMessages = await _outboxService.GetUnprocessedMessagesAsync(10);

                return Ok(new
                {
                    UnprocessedCount = unprocessedCount,
                    RecentUnprocessedMessages = unprocessedMessages.Select(m => new
                    {
                        m.Id,
                        m.Type,
                        m.CreatedAt,
                        m.IsProcessed
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox status");
                return StatusCode(500, new { Error = "Failed to get outbox status" });
            }
        }
    }

    public class PolicyCreatedRequest
    {
        public string PolicyId { get; set; } = string.Empty;
        public string PolicyNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public decimal PremiumAmount { get; set; }
    }
}