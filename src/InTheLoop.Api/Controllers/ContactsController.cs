using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InTheLoop.Api.Data;
using InTheLoop.Api.Models;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContactsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ContactsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get list of conversations (DMs and family chats) the user has participated in,
    /// with the last message in each conversation.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetConversations()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Get all DM messages where user is sender or recipient
        var dmMessages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.SenderId == userId || m.RecipientId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Get all family messages where user is a member
        var memberFamilyIds = await _context.FamilyMembers
            .Where(fm => fm.UserId == userId)
            .Select(fm => fm.FamilyId)
            .ToListAsync();

        var allFamilyMessages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.FamilyId.HasValue)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        var familyMessages = allFamilyMessages
            .Where(m => memberFamilyIds.Contains(m.FamilyId!.Value))
            .ToList();

        // Combine and deduplicate
        var allMessages = dmMessages.Concat(familyMessages)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .ToList();

        // Group by conversation partner or family in memory
        var conversationMap = new Dictionary<string, List<Message>>();

        foreach (var msg in allMessages)
        {
            string key;

            if (msg.FamilyId.HasValue)
            {
                key = $"family-{msg.FamilyId.Value}";
            }
            else if (msg.RecipientId != null)
            {
                var otherId = msg.SenderId == userId ? msg.RecipientId : msg.SenderId;
                key = $"dm-{otherId}";
            }
            else
            {
                continue;
            }

            if (!conversationMap.ContainsKey(key))
                conversationMap[key] = new List<Message>();

            conversationMap[key].Add(msg);
        }

        // Build result
        var result = new List<dynamic>();

        foreach (var kvp in conversationMap)
        {
            var key = kvp.Key;
            var messages = kvp.Value;
            var lastMsg = messages.OrderByDescending(m => m.SentAt).First();

            string partnerName, partnerAvatar;
            string conversationType;
            int? conversationId = null;

            if (key.StartsWith("family-"))
            {
                var familyId = int.Parse(key.Substring(7));
                var family = _context.Families.FirstOrDefault(f => f.Id == familyId);
                partnerName = family?.Name ?? "Unknown Family";
                partnerAvatar = family?.CoverPhotoUrl;
                conversationType = "family";
                conversationId = familyId;
            }
            else
            {
                var otherId = key.Substring(3);
                var otherUser = _context.Users.FirstOrDefault(u => u.Id == otherId);
                partnerName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "Unknown";
                partnerAvatar = otherUser?.AvatarUrl;
                conversationType = "dm";
                conversationId = null;
            }

            // Count unread DMs
            int unreadCount = 0;
            if (lastMsg.FamilyId == null && lastMsg.RecipientId == userId)
            {
                unreadCount = messages.Count(m => m.RecipientId == userId && !m.IsRead);
            }

            result.Add(new
            {
                id = key,
                conversationType,
                conversationId,
                partnerName,
                partnerAvatar,
                lastMessage = lastMsg.Content,
                lastMessageTime = lastMsg.SentAt,
                unreadCount
            });
        }

        result = result.OrderByDescending(c => c.lastMessageTime).ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get messages for a specific conversation (DM or family).
    /// </summary>
    [HttpGet("{conversationId}")]
    public async Task<IActionResult> GetConversationMessages(string conversationId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        IEnumerable<Message> messages;

        if (conversationId.StartsWith("family-"))
        {
            var familyId = int.Parse(conversationId.Substring(7));
            messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.FamilyId == familyId)
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
        }
        else if (conversationId.StartsWith("dm-"))
        {
            var otherId = conversationId.Substring(3);
            messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m =>
                    ((m.SenderId == userId && m.RecipientId == otherId) ||
                     (m.SenderId == otherId && m.RecipientId == userId))
                )
                .OrderBy(m => m.SentAt)
                .Take(100)
                .ToListAsync();
        }
        else
        {
            return BadRequest("Invalid conversation ID");
        }

        // Mark DMs as read
        if (!conversationId.StartsWith("family-"))
        {
            var otherId = conversationId.Substring(3);
            _context.Messages
                .Where(m => m.SenderId == otherId && m.RecipientId == userId && !m.IsRead)
                .ToList()
                .ForEach(m => m.IsRead = true);
            await _context.SaveChangesAsync();
        }

        var result = messages.Select(m => new
        {
            m.Id,
            m.Content,
            m.SentAt,
            isOwn = m.SenderId == userId,
            senderName = m.Sender != null ? $"{m.Sender.FirstName} {m.Sender.LastName}" : "Unknown"
        });

        return Ok(result);
    }
}
