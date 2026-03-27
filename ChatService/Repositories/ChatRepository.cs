using Microsoft.EntityFrameworkCore;
using ChatService.Data;
using ChatService.Models;

namespace ChatService.Repositories;

public class ChatRepository : IChatRepository
{
    private readonly ChatDbContext _context;
    private readonly ILogger<ChatRepository> _logger;

    public ChatRepository(ChatDbContext context, ILogger<ChatRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // Chat Messages
    public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
    {
        try
        {
            _context.ChatMessages.Add(message);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Message saved: {MessageId} from {SenderId}", message.Id, message.SenderId);
            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving message from {SenderId}", message.SenderId);
            throw;
        }
    }

    public async Task<List<ChatMessage>> GetGroupMessagesAsync(int limit = 50, int offset = 0)
    {
        return await _context.ChatMessages
            .Where(m => m.MessageType == MessageType.GroupMessage || m.MessageType == MessageType.SystemMessage)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> GetPrivateMessagesAsync(string agentId1, string agentId2, int limit = 50, int offset = 0)
    {
        return await _context.ChatMessages
            .Where(m => m.MessageType == MessageType.PrivateMessage &&
                       ((m.SenderId == agentId1 && m.ReceiverId == agentId2) ||
                        (m.SenderId == agentId2 && m.ReceiverId == agentId1)))
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ChatMessage>> GetMessagesByAgentAsync(string agentId, int limit = 50, int offset = 0)
    {
        return await _context.ChatMessages
            .Where(m => m.SenderId == agentId || m.ReceiverId == agentId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkMessagesAsReadAsync(string receiverId, string senderId)
    {
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.ReceiverId == receiverId && m.SenderId == senderId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Marked {Count} messages as read for {ReceiverId} from {SenderId}", 
            unreadMessages.Count, receiverId, senderId);
    }

    public async Task<int> GetUnreadMessageCountAsync(string agentId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == agentId && !m.IsRead);
    }

    // Agents
    public async Task<Agent> UpsertAgentAsync(string agentId, string agentName, AgentStatus status)
    {
        var existingAgent = await _context.Agents
            .FirstOrDefaultAsync(a => a.AgentId == agentId);

        if (existingAgent != null)
        {
            existingAgent.AgentName = agentName;
            existingAgent.Status = status;
            existingAgent.LastSeen = DateTime.UtcNow;
            _context.Agents.Update(existingAgent);
        }
        else
        {
            existingAgent = new Agent
            {
                AgentId = agentId,
                AgentName = agentName,
                Status = status,
                LastSeen = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _context.Agents.Add(existingAgent);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Agent upserted: {AgentId} - {AgentName} - {Status}", agentId, agentName, status);
        return existingAgent;
    }

    public async Task<List<Agent>> GetOnlineAgentsAsync()
    {
        return await _context.Agents
            .Where(a => a.Status == AgentStatus.Online)
            .OrderBy(a => a.AgentName)
            .ToListAsync();
    }

    public async Task<Agent?> GetAgentAsync(string agentId)
    {
        return await _context.Agents
            .FirstOrDefaultAsync(a => a.AgentId == agentId);
    }

    public async Task UpdateAgentStatusAsync(string agentId, AgentStatus status)
    {
        var agent = await _context.Agents
            .FirstOrDefaultAsync(a => a.AgentId == agentId);

        if (agent != null)
        {
            agent.Status = status;
            agent.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Agent status updated: {AgentId} - {Status}", agentId, status);
        }
    }

    public async Task UpdateAgentLastSeenAsync(string agentId)
    {
        var agent = await _context.Agents
            .FirstOrDefaultAsync(a => a.AgentId == agentId);

        if (agent != null)
        {
            agent.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}