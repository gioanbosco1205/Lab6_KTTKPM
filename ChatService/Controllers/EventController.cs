using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ChatService.Services;
using ChatService.Events;
using ChatService.Data;

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

        /// <summary>
        /// ⭐ Kiểm tra Messages table với CreatedAt và ProcessedAt
        /// </summary>
        [HttpGet("messages/status")]
        public async Task<IActionResult> GetMessagesStatus([FromServices] ChatDbContext context)
        {
            try
            {
                var totalCount = await context.Messages.CountAsync();
                var unprocessedCount = await context.Messages.CountAsync(m => m.ProcessedAt == null);
                var processedCount = await context.Messages.CountAsync(m => m.ProcessedAt != null);

                var oldestUnprocessed = await context.Messages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new { m.Id, m.Type, m.CreatedAt })
                    .FirstOrDefaultAsync();

                var recentMessages = await context.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Take(10)
                    .Select(m => new
                    {
                        m.Id,
                        m.Type,
                        m.CreatedAt,
                        m.ProcessedAt,
                        IsProcessed = m.ProcessedAt != null,
                        ProcessingTimeSeconds = m.ProcessedAt != null 
                            ? (m.ProcessedAt.Value - m.CreatedAt).TotalSeconds 
                            : (double?)null
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalMessages = totalCount,
                    UnprocessedCount = unprocessedCount,
                    ProcessedCount = processedCount,
                    OldestUnprocessed = oldestUnprocessed,
                    RecentMessages = recentMessages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get messages status");
                return StatusCode(500, new { Error = "Failed to get messages status" });
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