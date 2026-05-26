namespace InTheLoop.Api.Models;

public class Family
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverPhotoUrl { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public User Owner { get; set; } = null!;
    public ICollection<FamilyMember> Members { get; set; } = new List<FamilyMember>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
