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
            .Where(p => p.FamilyId == familyId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return posts.Select(MapToDto);
    }

    private PostDto MapToDto(Post post)
    {
        // Look up author name and family name from the context since navigation properties
        // may not be loaded in the Post entity
        var authorName = _context.Users
            .Where(u => u.Id == post.AuthorId)
            .Select(u => $"{u.FirstName} {u.LastName}")
            .FirstOrDefault() ?? "";

        var familyName = _context.Families
            .Where(f => f.Id == post.FamilyId)
            .Select(f => f.Name)
            .FirstOrDefault() ?? "";

        var photoUrls = _context.Photos
            .Where(p => p.PostId == post.Id)
            .Select(p => p.Url)
            .ToList();

        return new PostDto(
            post.Id,
            post.Content,
            authorName,
            _context.Users.Where(u => u.Id == post.AuthorId).Select(u => u.AvatarUrl).FirstOrDefault(),
            post.FamilyId,
            familyName,
            photoUrls,
            post.CreatedAt);
    }
}
