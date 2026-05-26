using InTheLoop.Api.Data;
using InTheLoop.Api.DTOs;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class PostService
{
    private readonly ApplicationDbContext _context;

    public PostService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PostDto> CreatePostAsync(string authorId, int familyId, string content)
    {
        var post = new Post
        {
            AuthorId = authorId,
            FamilyId = familyId,
            Content = content
        };

        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return MapToDto(post);
    }

    public async Task<IEnumerable<PostDto>> GetFamilyFeedAsync(int familyId, int skip = 0, int take = 20)
    {
        var posts = await _context.Posts
            .Include(p => p.Author)
            .Include(p => p.Photos)
            .Include(p => p.Family)
            .Where(p => p.FamilyId == familyId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return posts.Select(MapToDto);
    }

    private PostDto MapToDto(Post post)
    {
        return new PostDto(
            post.Id,
            post.Content,
            $"{post.Author.FirstName} {post.Author.LastName}",
            post.Author.AvatarUrl,
            post.FamilyId,
            post.Family.Name,
            post.Photos.Select(p => p.Url).ToList(),
            post.CreatedAt);
    }
}
