using Microsoft.AspNetCore.Mvc;
using ChatService.Services;
using ChatService.Events;

namespace ChatService.Controllers
{
    /// <summary>
    /// Demo Controller minh họa cách sử dụng IEventPublisher với Dependency Injection
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyDemoController : ControllerBase
    {
        private readonly IEventPublisher _eventPublisher;
        private readonly ITransactionalEventService _transactionalEventService;
        private readonly IOutboxService _outboxService;
        private readonly ILogger<PolicyDemoController> _logger;

        // ✅ Dependency Injection - Inject các services cần thiết
        public PolicyDemoController(
            IEventPublisher eventPublisher,
            ITransactionalEventService transactionalEventService,
            IOutboxService outboxService,
            ILogger<PolicyDemoController> logger)
        {
            _eventPublisher = eventPublisher;
            _transactionalEventService = transactionalEventService;
            _outboxService = outboxService;
            _logger = logger;
        }

        /// <summary>
        /// Demo 1: Sử dụng IEventPublisher trực tiếp
        /// services.AddScoped&lt;IEventPublisher, OutboxEventPublisher&gt;();
        /// </summary>
        [HttpPost("create-policy-simple")]
        public async Task<IActionResult> CreatePolicySimple([FromBody] CreatePolicyRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating policy: {request.PolicyNumber}");

                // 1. Business logic (giả lập tạo policy)
                var policy = new
                {
                    Id = Guid.NewGuid(),
                    PolicyNumber = request.PolicyNumber,
                    Premium = request.Premium,
                    Status = "Created",
                    CreatedAt = DateTime.UtcNow
                };

                // 2. Publish event sử dụng OutboxEventPublisher
                var policyCreatedEvent = new PolicyCreated
                {
                    PolicyNumber = policy.PolicyNumber,
                    Premium = policy.Premium,
                    Status = policy.Status,
                    CreatedAt = policy.CreatedAt
                };

                // ✅ Sử dụng IEventPublisher (đã được inject)
                await _eventPublisher.PublishMessage(policyCreatedEvent);

                _logger.LogInformation($"Policy {policy.PolicyNumber} created and event published to outbox");

                return Ok(new
                {
                    Message = "Policy created successfully",
                    Policy = policy,
                    EventPublished = true,
                    Note = "Event saved to outbox, will be processed by background service"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create policy");
                return StatusCode(500, new { Error = "Failed to create policy" });
            }
        }

        /// <summary>
        /// Demo 2: Sử dụng ITransactionalEventService (Khuyến nghị)
        /// Đảm bảo tính nhất quán giữa dữ liệu và events
        /// </summary>
        [HttpPost("create-policy-transactional")]
        public async Task<IActionResult> CreatePolicyTransactional([FromBody] CreatePolicyRequest request)
        {
            try
            {
                _logger.LogInformation($"Creating policy with transaction: {request.PolicyNumber}");

                var policy = new
                {
                    Id = Guid.NewGuid(),
                    PolicyNumber = request.PolicyNumber,
                    Premium = request.Premium,
                    Status = "Created",
                    CreatedAt = DateTime.UtcNow
                };

                var policyCreatedEvent = new PolicyCreated
                {
                    PolicyNumber = policy.PolicyNumber,
                    Premium = policy.Premium,
                    Status = policy.Status,
                    CreatedAt = policy.CreatedAt
                };

                // ✅ Sử dụng TransactionalEventService để đảm bảo consistency
                await _transactionalEventService.SaveDataAndPublishEventAsync(
                    async (context) =>
                    {
                        // Giả lập lưu policy vào database
                        // Trong thực tế sẽ là: context.Policies.Add(policy);
                        _logger.LogInformation($"Saving policy {policy.PolicyNumber} to database");
                        await Task.Delay(100); // Simulate database operation
                    },
                    policyCreatedEvent
                );

                return Ok(new
                {
                    Message = "Policy created with transactional consistency",
                    Policy = policy,
                    EventPublished = true,
                    Note = "Both policy data and event saved in same transaction"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create policy with transaction");
                return StatusCode(500, new { Error = "Failed to create policy" });
            }
        }

        /// <summary>
        /// Demo 3: Publish multiple events
        /// </summary>
        [HttpPost("create-policy-with-activation")]
        public async Task<IActionResult> CreatePolicyWithActivation([FromBody] CreatePolicyRequest request)
        {
            try
            {
                var policy = new
                {
                    Id = Guid.NewGuid(),
                    PolicyNumber = request.PolicyNumber,
                    Premium = request.Premium,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                // Event 1: Policy Created
                var policyCreatedEvent = new PolicyCreated
                {
                    PolicyNumber = policy.PolicyNumber,
                    Premium = policy.Premium,
                    Status = "Created",
                    CreatedAt = policy.CreatedAt
                };

                // Event 2: Product Activated
                var productActivatedEvent = new ProductActivated
                {
                    PolicyId = policy.Id.ToString(),
                    ProductId = request.ProductId,
                    ActivatedAt = DateTime.UtcNow
                };

                // ✅ Publish multiple events
                await _eventPublisher.PublishMessage(policyCreatedEvent);
                await _eventPublisher.PublishMessage(productActivatedEvent);

                return Ok(new
                {
                    Message = "Policy created and activated",
                    Policy = policy,
                    EventsPublished = 2,
                    Events = new[] { nameof(PolicyCreated), nameof(ProductActivated) }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create and activate policy");
                return StatusCode(500, new { Error = "Failed to create policy" });
            }
        }

        /// <summary>
        /// Monitor outbox status
        /// </summary>
        [HttpGet("outbox-status")]
        public async Task<IActionResult> GetOutboxStatus()
        {
            try
            {
                var unprocessedCount = await _outboxService.GetUnprocessedCountAsync();
                var recentMessages = await _outboxService.GetUnprocessedMessagesAsync(5);

                return Ok(new
                {
                    UnprocessedCount = unprocessedCount,
                    RecentUnprocessedMessages = recentMessages.Select(m => new
                    {
                        m.Id,
                        m.Type,
                        m.CreatedAt,
                        m.IsProcessed,
                        PayloadPreview = m.JsonPayload.Length > 100 
                            ? m.JsonPayload.Substring(0, 100) + "..." 
                            : m.JsonPayload
                    }),
                    Note = "OutboxProcessorService will process these messages in background"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox status");
                return StatusCode(500, new { Error = "Failed to get outbox status" });
            }
        }
    }

    public class CreatePolicyRequest
    {
        public string PolicyNumber { get; set; } = string.Empty;
        public decimal Premium { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
    }
}