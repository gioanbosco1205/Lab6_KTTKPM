using Microsoft.AspNetCore.Mvc;
using ChatService.Services;

namespace ChatService.Controllers
{
    /// <summary>
    /// Controller để monitor OutboxSendingService
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class OutboxMonitorController : ControllerBase
    {
        private readonly IOutbox _outbox;
        private readonly ILogger<OutboxMonitorController> _logger;

        public OutboxMonitorController(IOutbox outbox, ILogger<OutboxMonitorController> logger)
        {
            _outbox = outbox;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra trạng thái outbox
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetOutboxStatus()
        {
            try
            {
                var unprocessedCount = await _outbox.GetUnprocessedCountAsync();
                var recentMessages = await _outbox.GetUnprocessedMessagesAsync(10);

                return Ok(new
                {
                    Status = "OutboxSendingService running every 1 second",
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
                    ServiceInfo = new
                    {
                        ServiceName = "OutboxSendingService",
                        Interval = "1 second",
                        Description = "IHostedService running background job to push outbox messages"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox status");
                return StatusCode(500, new { Error = "Failed to get outbox status" });
            }
        }

        /// <summary>
        /// Thống kê outbox performance
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetOutboxStats()
        {
            try
            {
                var unprocessedCount = await _outbox.GetUnprocessedCountAsync();
                var allMessages = await _outbox.GetUnprocessedMessagesAsync(1000);

                var stats = new
                {
                    TotalUnprocessed = unprocessedCount,
                    OldestUnprocessedMessage = allMessages.FirstOrDefault()?.CreatedAt,
                    MessagesByType = allMessages
                        .GroupBy(m => m.Type)
                        .Select(g => new { Type = g.Key, Count = g.Count() })
                        .ToList(),
                    ServiceConfiguration = new
                    {
                        ServiceType = "IHostedService",
                        ProcessingInterval = "1 second",
                        BatchSize = 5,
                        AutoRetry = true
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get outbox stats");
                return StatusCode(500, new { Error = "Failed to get outbox stats" });
            }
        }
    }
}