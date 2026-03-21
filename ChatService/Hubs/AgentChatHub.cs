using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace ChatService.Hubs;

[Authorize]
public class AgentChatHub : Hub
{
    public async Task SendMessage(string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", Context.User.Identity.Name, message);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Others.SendAsync("ReceiveNotification", $"{Context.User.Identity.Name} joined chat");
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        await Clients.Others.SendAsync("ReceiveNotification", $"{Context.User.Identity.Name} left chat");
    }
}