using ChatService.Models;

namespace ChatService.Repositories;

public interface IChatRepository
{
    // Chat Messages
    Task<ChatMessage> SaveMessageAsync(ChatMessage message);
    Task<List<ChatMessage>> GetGroupMessagesAsync(int limit = 50, int offset = 0);
    Task<List<ChatMessage>> GetPrivateMessagesAsync(string agentId1, string agentId2, int limit = 50, int offset = 0);
    Task<List<ChatMessage>> GetMessagesByAgentAsync(string agentId, int limit = 50, int offset = 0);
    Task MarkMessagesAsReadAsync(string receiverId, string senderId);
    Task<int> GetUnreadMessageCountAsync(string agentId);

    // Agents
    Task<Agent> UpsertAgentAsync(string agentId, string agentName, AgentStatus status);
    Task<List<Agent>> GetOnlineAgentsAsync();
    Task<Agent?> GetAgentAsync(string agentId);
    Task UpdateAgentStatusAsync(string agentId, AgentStatus status);
    Task UpdateAgentLastSeenAsync(string agentId);
}