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
        var messages = _context.Messages
            .Include(m => m.Sender)
            .Where(m =>
                (m.SenderId == user1Id && m.RecipientId == user2Id) ||
                (m.SenderId == user2Id && m.RecipientId == user1Id) ||
                (m.FamilyId == familyId))
            .OrderBy(m => m.SentAt)
            .AsQueryable();

        return await messages.Take(100).ToListAsync();
    }
}
