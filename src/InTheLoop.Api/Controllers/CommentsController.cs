using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InTheLoop.Api.Services;
using InTheLoop.Api.Models;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly CommentService _commentService;

    public CommentsController(CommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet("posts/{postId}")]
    public async Task<IActionResult> GetPostComments(int postId)
    {
        var comments = await _commentService.GetCommentsAsync(postId);
        return Ok(comments);
    }

    [HttpPost("posts/{postId}")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] AddCommentRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var comment = await _commentService.AddCommentAsync(userId, postId, request.Content);
        return Ok(comment);
    }

    [HttpPost("{commentId}/reply")]
    public async Task<IActionResult> ReplyToComment(int commentId, [FromBody] AddReplyRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var comment = await _commentService.AddCommentAsync(userId, request.PostId, request.Content, commentId);
        return Ok(comment);
    }
}

public record AddCommentRequest(string Content);
public record AddReplyRequest(int PostId, string Content);
