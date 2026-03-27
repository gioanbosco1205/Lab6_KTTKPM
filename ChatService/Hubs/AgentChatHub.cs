using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using ChatService.Repositories;
using ChatService.Models;

namespace ChatService.Hubs;

public class AgentChatHub : Hub
{
    private readonly IChatRepository _chatRepository;
    private readonly ILogger<AgentChatHub> _logger;

    public AgentChatHub(IChatRepository chatRepository, ILogger<AgentChatHub> logger)
    {
        _chatRepository = chatRepository;
        _logger = logger;
    }

    public async Task SendMessage(string message)
    {
        var senderName = Context.User?.Identity?.Name ?? "Anonymous";
        var senderId = Context.User?.FindFirst("userId")?.Value ?? "Unknown";

        // Save to database
        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            SenderName = senderName,
            Message = message,
            MessageType = MessageType.GroupMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepository.SaveMessageAsync(chatMessage);

        // Send to all clients
        await Clients.All.SendAsync("ReceiveMessage", senderName, message);
    }

    public async Task SendMessageToAgents(string message)
    {
        var senderName = Context.User?.Identity?.Name ?? "Anonymous";
        var senderId = Context.User?.FindFirst("userId")?.Value ?? "Unknown";

        // Save to database
        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            SenderName = senderName,
            Message = message,
            MessageType = MessageType.GroupMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepository.SaveMessageAsync(chatMessage);

        // Send to agents group
        await Clients.Group("Agents").SendAsync("ReceiveMessage", senderName, message);
    }

    public async Task SendPrivateMessage(string targetAgentId, string message)
    {
        var senderName = Context.User?.Identity?.Name ?? "Anonymous";
        var senderId = Context.User?.FindFirst("userId")?.Value ?? "Unknown";
        
        // Get target agent info
        var targetAgent = await _chatRepository.GetAgentAsync(targetAgentId);
        var targetAgentName = targetAgent?.AgentName ?? targetAgentId;

        // Save to database
        var chatMessage = new ChatMessage
        {
            SenderId = senderId,
            SenderName = senderName,
            ReceiverId = targetAgentId,
            ReceiverName = targetAgentName,
            Message = message,
            MessageType = MessageType.PrivateMessage,
            CreatedAt = DateTime.UtcNow
        };

        await _chatRepository.SaveMessageAsync(chatMessage);
        
        // Send to target agent
        await Clients.Group($"Agent_{targetAgentId}").SendAsync("ReceivePrivateMessage", senderName, senderId, message);
        
        // Send confirmation to sender
        await Clients.Caller.SendAsync("PrivateMessageSent", targetAgentId, message);
    }

    public async Task GetOnlineAgents()
    {
        try
        {
            var onlineAgents = await _chatRepository.GetOnlineAgentsAsync();
            var agentList = onlineAgents.Select(a => new 
            { 
                AgentId = a.AgentId, 
                AgentName = a.AgentName, 
                Status = a.Status.ToString() 
            }).ToArray();
            
            await Clients.Caller.SendAsync("OnlineAgentsList", agentList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online agents");
            // Fallback to mock data
            var onlineAgents = new[]
            {
                new { AgentId = "agent1", AgentName = "Agent Smith", Status = "Online" },
                new { AgentId = "agent2", AgentName = "Agent Johnson", Status = "Online" },
                new { AgentId = "agent3", AgentName = "Agent Brown", Status = "Away" }
            };
            
            await Clients.Caller.SendAsync("OnlineAgentsList", onlineAgents);
        }
    }

    public async Task JoinAgentGroup(string agentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Agents");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Agent_{agentId}");
        
        // Update agent status in database
        var agentName = Context.User?.Identity?.Name ?? agentId;
        await _chatRepository.UpsertAgentAsync(agentId, agentName, AgentStatus.Online);
        
        await Clients.Group("Agents").SendAsync("AgentJoined", agentId);
        await Clients.Caller.SendAsync("ReceiveNotification", $"Joined as agent {agentId}");
    }

    public async Task LeaveAgentGroup(string agentId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Agents");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Agent_{agentId}");
        
        // Update agent status in database
        await _chatRepository.UpdateAgentStatusAsync(agentId, AgentStatus.Offline);
        
        await Clients.Group("Agents").SendAsync("AgentLeft", agentId);
    }

    // ⭐ PHẦN 14 - SignalR Notification Methods
    public async Task JoinNotificationGroup(string groupName = "PolicyNotifications")
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("ReceiveNotification", $"Joined {groupName} group for real-time notifications");
    }

    public async Task LeaveNotificationGroup(string groupName = "PolicyNotifications")
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("ReceiveNotification", $"Left {groupName} group");
    }

    public override async Task OnConnectedAsync()
    {
        var userName = Context.User?.Identity?.Name ?? "Anonymous User";
        var userId = Context.User?.FindFirst("userId")?.Value ?? "Unknown";
        
        await Clients.Others.SendAsync("ReceiveNotification", $"🟢 {userName} connected to chat");
        
        // Tự động join notification group
        await Groups.AddToGroupAsync(Context.ConnectionId, "PolicyNotifications");
        await Clients.Caller.SendAsync("ReceiveNotification", "✅ Connected and ready to receive policy notifications");
        
        // Notify other agents about new agent online
        await Clients.Group("Agents").SendAsync("AgentStatusChanged", userId, userName, "Online");
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "Anonymous User";
        var userId = Context.User?.FindFirst("userId")?.Value ?? "Unknown";
        
        // Update agent status in database
        if (userId != "Unknown")
        {
            await _chatRepository.UpdateAgentStatusAsync(userId, AgentStatus.Offline);
        }
        
        await Clients.Others.SendAsync("ReceiveNotification", $"🔴 {userName} disconnected from chat");
        
        // Notify other agents about agent going offline
        await Clients.Group("Agents").SendAsync("AgentStatusChanged", userId, userName, "Offline");
        
        await base.OnDisconnectedAsync(exception);
    }
}