namespace InTheLoop.Api.Models;

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public User Author { get; set; } = null!;
    public int FamilyId { get; set; }
    public Family Family { get; set; } = null!;
    public ICollection<Photo> Photos { get; set; } = new List<Photo>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
