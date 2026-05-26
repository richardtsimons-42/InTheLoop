using Microsoft.AspNetCore.SignalR;

namespace InTheLoop.Api.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveMessage", message);
    }

    public async Task JoinFamilyChat(int familyId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"family-{familyId}");
    }

    public async Task LeaveFamilyChat(int familyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"family-{familyId}");
    }
}
