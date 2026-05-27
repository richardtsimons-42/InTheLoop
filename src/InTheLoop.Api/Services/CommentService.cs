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

        // Load navigation properties
        await _context.Entry(comment).Reference(c => c.Author).LoadAsync();
        if (parentCommentId.HasValue)
        {
            await _context.Entry(comment).Reference(c => c.ParentComment).LoadAsync();
        }

        return comment;
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync(int postId)
    {
        // Load ALL comments for the post with full nesting
        var allComments = await _context.Comments
            .Where(c => c.PostId == postId)
            .Include(c => c.Author)
            .Include(c => c.Replies)
                .ThenInclude(r => r.Author)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();

        // Build nested structure: group by parentCommentId
        var commentDict = allComments.ToDictionary(c => c.Id);
        var topLevel = new List<Comment>();

        foreach (var comment in allComments)
        {
            if (comment.ParentCommentId == null || !commentDict.ContainsKey(comment.ParentCommentId.Value))
            {
                // Top-level comment or parent not found
                topLevel.Add(comment);
            }
            else
            {
                // Add to parent's replies list
                var parent = commentDict[comment.ParentCommentId.Value];
                if (!parent.Replies.Any(r => r.Id == comment.Id))
                {
                    parent.Replies.Add(comment);
                }
            }
        }

        return topLevel;
    }

    public async Task<Comment?> GetCommentAsync(int commentId)
    {
        return await _context.Comments
            .Include(c => c.Author)
            .Include(c => c.ParentComment)
            .FirstOrDefaultAsync(c => c.Id == commentId);
    }
}
