namespace InTheLoop.Api.Models;

public class Message
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public User Sender { get; set; } = null!;
    public string? RecipientId { get; set; }
    public User? Recipient { get; set; }
    public int? FamilyId { get; set; }
    public Family? Family { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}
