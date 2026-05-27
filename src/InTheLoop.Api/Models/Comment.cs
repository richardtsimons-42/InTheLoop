namespace InTheLoop.Api.Models;

public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public User Author { get; set; } = null!;
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public int? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
