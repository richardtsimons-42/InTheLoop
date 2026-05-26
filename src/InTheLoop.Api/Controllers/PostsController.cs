using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InTheLoop.Api.Services;
using InTheLoop.Api.DTOs;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly PostService _postService;
    private readonly PhotoService _photoService;

    public PostsController(PostService postService, PhotoService photoService)
    {
        _postService = postService;
        _photoService = photoService;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var post = await _postService.CreatePostAsync(userId, request.FamilyId, request.Content);
        return Ok(post);
    }

    [HttpPost("{postId}/photos")]
    public async Task<IActionResult> UploadPhoto(int postId, [FromForm] IFormFile file)
    {
        var url = await _photoService.UploadPhotoAsync(postId, file);
        return Ok(new { url });
    }

    [HttpGet("{familyId}/feed")]
    public async Task<IActionResult> GetFeed(int familyId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var posts = await _postService.GetFamilyFeedAsync(familyId, skip, take);
        return Ok(posts);
    }
}

public record CreatePostRequest(int FamilyId, string Content);
