namespace InTheLoop.Api.Models;

public class Photo
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
