using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ChatService.Hubs;

public class AgentChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", Context.User?.Identity?.Name ?? "Anonymous", message);
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
        await Clients.Others.SendAsync("ReceiveNotification", $"🟢 {userName} connected to chat");
        
        // Tự động join notification group
        await Groups.AddToGroupAsync(Context.ConnectionId, "PolicyNotifications");
        await Clients.Caller.SendAsync("ReceiveNotification", "✅ Connected and ready to receive policy notifications");
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "Anonymous User";
        await Clients.Others.SendAsync("ReceiveNotification", $"🔴 {userName} disconnected from chat");
        
        await base.OnDisconnectedAsync(exception);
    }
}