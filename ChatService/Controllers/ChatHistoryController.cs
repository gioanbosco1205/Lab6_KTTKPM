using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ChatService.Repositories;
using ChatService.Models;

namespace ChatService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatHistoryController : ControllerBase
{
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<ChatHistoryController> _logger;

    public ChatHistoryController(IChatRepository chatRepository, ILogger<ChatHistoryController> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;
    }

    [HttpGet("group-messages")]
    public async Task<ActionResult<List<ChatMessage>>> GetGroupMessages(
        [FromQuery] int limit = 50, 
        [FromQuery] int offset = 0)
    {
        try
        {
            var messages = await _chatRepository.GetGroupMessagesAsync(limit, offset);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group messages");
            return StatusCode(500, new { error = "Failed to get group messages" });
        }
    }

    [HttpGet("private-messages/{otherAgentId}")]
    public async Task<ActionResult<List<ChatMessage>>> GetPrivateMessages(
        string otherAgentId,
        [FromQuery] int limit = 50, 
        [FromQuery] int offset = 0)
    {
        try
        {
            var currentAgentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentAgentId))
            {
                return Unauthorized(new { error = "Agent ID not found in token" });
            }

            var messages = await _chatRepository.GetPrivateMessagesAsync(currentAgentId, otherAgentId, limit, offset);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting private messages between {AgentId1} and {AgentId2}", 
                User.FindFirst("userId")?.Value, otherAgentId);
            return StatusCode(500, new { error = "Failed to get private messages" });
        }
    }

    [HttpGet("my-messages")]
    public async Task<ActionResult<List<ChatMessage>>> GetMyMessages(
        [FromQuery] int limit = 50, 
        [FromQuery] int offset = 0)
    {
        try
        {
            var currentAgentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentAgentId))
            {
                return Unauthorized(new { error = "Agent ID not found in token" });
            }

            var messages = await _chatRepository.GetMessagesByAgentAsync(currentAgentId, limit, offset);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for agent {AgentId}", User.FindFirst("userId")?.Value);
            return StatusCode(500, new { error = "Failed to get messages" });
        }
    }

    [HttpPost("mark-read/{senderId}")]
    public async Task<IActionResult> MarkMessagesAsRead(string senderId)
    {
        try
        {
            var currentAgentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentAgentId))
            {
                return Unauthorized(new { error = "Agent ID not found in token" });
            }

            await _chatRepository.MarkMessagesAsReadAsync(currentAgentId, senderId);
            return Ok(new { message = "Messages marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking messages as read for {ReceiverId} from {SenderId}", 
                User.FindFirst("userId")?.Value, senderId);
            return StatusCode(500, new { error = "Failed to mark messages as read" });
        }
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadMessageCount()
    {
        try
        {
            var currentAgentId = User.FindFirst("userId")?.Value;
            if (string.IsNullOrEmpty(currentAgentId))
            {
                return Unauthorized(new { error = "Agent ID not found in token" });
            }

            var count = await _chatRepository.GetUnreadMessageCountAsync(currentAgentId);
            return Ok(new { unreadCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for agent {AgentId}", User.FindFirst("userId")?.Value);
            return StatusCode(500, new { error = "Failed to get unread count" });
        }
    }

    [HttpGet("online-agents")]
    public async Task<ActionResult<List<Agent>>> GetOnlineAgents()
    {
        try
        {
            var agents = await _chatRepository.GetOnlineAgentsAsync();
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online agents");
            return StatusCode(500, new { error = "Failed to get online agents" });
        }
    }
}