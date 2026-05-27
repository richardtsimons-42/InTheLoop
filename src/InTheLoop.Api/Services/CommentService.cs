using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class CommentService
{
    private readonly ApplicationDbContext _context;

    public CommentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Comment> AddCommentAsync(string authorId, int postId, string content, int? parentCommentId = null)
    {
        var comment = new Comment
        {
            AuthorId = authorId,
            PostId = postId,
            Content = content,
            ParentCommentId = parentCommentId
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Load navigation properties - avoid circular references
        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
        if (parentCommentId.HasValue)
        {
            await _context.Entry(comment).Reference(c => c.ParentComment).LoadAsync();
        }
        // Don't load Replies to avoid circular reference on reply creation
        await _context.Entry(comment).Collection(c => c.Replies).LoadAsync();

        return comment;
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync(int postId)
    {
        return await _context.Comments
            .Where(c => c.PostId == postId && c.ParentCommentId == null)
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetCommentAsync(int commentId)
    {
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.ParentComment)
            .FirstOrDefaultAsync(c => c.Id == commentId);
    }
}
