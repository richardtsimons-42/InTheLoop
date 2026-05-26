using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class MessageService
{
    private readonly ApplicationDbContext _context;

    public MessageService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SendMessageAsync(string senderId, string? recipientId, int? familyId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            RecipientId = recipientId,
            FamilyId = familyId,
            Content = content
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Message>> GetConversationAsync(string user1Id, string? user2Id, int? familyId)
    {
        List<Message> messageList;

        if (user2Id != null && familyId == null)
        {
            // DM conversation - EF Core InMemory doesn't support || in Where with Include,
            // so we query both directions separately and combine in-memory
            var fromUser1 = await _context.Messages
                .Where(m => m.SenderId == user1Id && m.RecipientId == user2Id)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
            var fromUser2 = await _context.Messages
                .Where(m => m.SenderId == user2Id && m.RecipientId == user1Id)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
            messageList = fromUser1.Concat(fromUser2)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToList();
        }
        else if (familyId != null)
        {
            // Family group chat
            messageList = await _context.Messages
                .Where(m => m.FamilyId == familyId)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
        }
        else
        {
            // No filter - return all messages
            messageList = await _context.Messages
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
        }

        // Manually load sender data (EF Core InMemory Include is unreliable)
        if (messageList.Any())
        {
            var senderIds = messageList.Select(m => m.SenderId).Distinct().ToList();
            var senders = await _context.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);
            foreach (var msg in messageList)
            {
                msg.Sender = senders.GetValueOrDefault(msg.SenderId);
            }
        }

        return messageList;
    }
}
