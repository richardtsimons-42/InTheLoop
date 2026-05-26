namespace InTheLoop.Api.Models;

public class FamilyMember
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public string Role { get; set; } = "member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
