using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using System.Collections.Concurrent;

namespace InTheLoop.Api.Hubs;

public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;
    private static readonly ConcurrentDictionary<string, string> _userConnections = new();

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections[userId] = Context.ConnectionId;
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            _userConnections.TryRemove(userId, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send a direct message to a specific user. Persists to DB and delivers via SignalR.
    /// </summary>
    public async Task SendDirectMessage(string recipientId, string content)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        // Save to DB
        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content,
            SentAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Deliver to recipient in real-time
        var recipientConnectionId = _userConnections.GetValueOrDefault(recipientId);
        if (recipientConnectionId != null)
        {
            await Clients.Client(recipientConnectionId).SendAsync("ReceiveMessage", new
            {
                message.Id,
                SenderId = senderId,
                SenderName = await GetUserNameAsync(senderId),
                content,
                sentAt = message.SentAt
            });
        }

        // Also deliver to sender (for their own UI)
        await Clients.Caller.SendAsync("ReceiveMessage", new
        {
            message.Id,
            SenderId = senderId,
            SenderName = await GetUserNameAsync(senderId),
            content,
            sentAt = message.SentAt
        });
    }

    /// <summary>
    /// Send a message to a family group chat. Persists to DB and broadcasts to all family members.
    /// </summary>
    public async Task SendFamilyMessage(int familyId, string content)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        // Verify user is a member of this family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == senderId && fm.FamilyId == familyId);
        if (!isMember) return;

        // Save to DB
        var message = new Message
        {
            SenderId = senderId,
            FamilyId = familyId,
            Content = content,
            SentAt = DateTime.UtcNow
        };
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        // Broadcast to all family members
        var groupKey = $"family-{familyId}";
        await Clients.Group(groupKey).SendAsync("ReceiveFamilyMessage", new
        {
            message.Id,
            SenderId = senderId,
            SenderName = await GetUserNameAsync(senderId),
            content,
            sentAt = message.SentAt
        });
    }

    /// <summary>
    /// Join a family chat group.
    /// </summary>
    public async Task JoinFamilyChat(int familyId)
    {
        var senderId = Context.UserIdentifier;
        if (string.IsNullOrEmpty(senderId)) return;

        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == senderId && fm.FamilyId == familyId);
        if (!isMember) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"family-{familyId}");
        await Clients.Group($"family-{familyId}").SendAsync("UserJoined", new
        {
            UserId = senderId,
            Name = await GetUserNameAsync(senderId)
        });
    }

    /// <summary>
    /// Leave a family chat group.
    /// </summary>
    public async Task LeaveFamilyChat(int familyId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"family-{familyId}");
    }

    private async Task<string> GetUserNameAsync(string userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? $"{user.FirstName} {user.LastName}" : "Unknown";
    }
}
