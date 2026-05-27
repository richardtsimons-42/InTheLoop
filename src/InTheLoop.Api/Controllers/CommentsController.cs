using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using InTheLoop.Api.Data;
using InTheLoop.Api.Services;
using InTheLoop.Api.Models;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly CommentService _commentService;
    private readonly ApplicationDbContext _context;

    public CommentsController(CommentService commentService, ApplicationDbContext context)
    {
        _commentService = commentService;
        _context = context;
    }

    [HttpGet("posts/{postId}")]
    public async Task<IActionResult> GetPostComments(int postId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var post = await _context.Posts
            .Include(p => p.Family)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return NotFound(new { message = "Post not found" });

        // Verify the user is a member of the family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == post.FamilyId);

        if (!isMember)
            return Forbid();

        var comments = await _commentService.GetCommentsAsync(postId);
        return Ok(comments);
    }

    [HttpPost("posts/{postId}")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] AddCommentRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var post = await _context.Posts
            .Include(p => p.Family)
            .FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return NotFound(new { message = "Post not found" });

        // Verify the user is a member of the family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == post.FamilyId);

        if (!isMember)
            return StatusCode(403, new { message = "You must be a family member to comment" });

        var comment = await _commentService.AddCommentAsync(userId, postId, request.Content);
        return Ok(comment);
    }

    [HttpPost("{commentId}/reply")]
    public async Task<IActionResult> ReplyToComment(int commentId, [FromBody] AddReplyRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        var post = await _context.Posts
            .Include(p => p.Family)
            .FirstOrDefaultAsync(p => p.Id == request.PostId);

        if (post == null)
            return NotFound(new { message = "Post not found" });

        // Verify the user is a member of the family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == post.FamilyId);

        if (!isMember)
            return StatusCode(403, new { message = "You must be a family member to reply" });

        var comment = await _commentService.AddCommentAsync(userId, request.PostId, request.Content, commentId);
        return Ok(comment);
    }
}

public record AddCommentRequest(string Content);
public record AddReplyRequest(int PostId, string Content);
