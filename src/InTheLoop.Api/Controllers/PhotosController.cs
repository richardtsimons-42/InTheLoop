using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhotosController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PhotosController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [HttpGet("family/{familyId}")]
    public async Task<IActionResult> GetFamilyPhotos(int familyId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Verify the user is a member of this family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == familyId);

        if (!isMember)
            return Forbid();

        var photos = await _context.Photos
            .Include(p => p.Post)
            .ThenInclude(p => p!.Author)
            .Where(p => p.Post!.FamilyId == familyId)
            .OrderByDescending(p => p.UploadedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var result = photos.Select(p => new
        {
            p.Id,
            p.Url,
            p.AltText,
            p.UploadedAt,
            PostId = p.PostId,
            PostAuthor = p.Post.Author != null
                ? $"{p.Post.Author.FirstName} {p.Post.Author.LastName}"
                : null,
            PostContent = p.Post.Content
        });

        return Ok(result);
    }

    [HttpGet("post/{postId}")]
    public async Task<IActionResult> GetPostPhotos(int postId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var photo = await _context.Photos
            .Include(p => p.Post)
            .FirstOrDefaultAsync(p => p.PostId == postId);

        if (photo == null || photo.Post == null)
            return NotFound();

        // Verify the user is a member of the family that owns the post
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == photo.Post.FamilyId);

        if (!isMember)
            return Forbid();

        var photos = await _context.Photos
            .Where(p => p.PostId == postId)
            .OrderBy(p => p.UploadedAt)
            .ToListAsync();

        return Ok(photos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var photo = await _context.Photos
            .Include(p => p.Post)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo == null || photo.Post == null)
            return NotFound(new { message = "Photo not found" });

        // Verify the user is a member of the family that owns the post
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == photo.Post.FamilyId);

        if (!isMember)
            return Forbid();

        return Ok(new
        {
            photo.Id,
            photo.Url,
            photo.AltText,
            photo.UploadedAt,
            PostId = photo.PostId
        });
    }

    [HttpGet("file/{fileName}")]
    public async Task<IActionResult> GetPhotoFile(string fileName)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Look up the photo to find its post and family
        var photo = await _context.Photos
            .Include(p => p.Post)
            .ThenInclude(p => p!.Family)
            .FirstOrDefaultAsync(p => p.Url.Contains(fileName));

        if (photo == null || photo.Post == null || photo.Post.Family == null)
            return NotFound();

        // Verify the user is a member of the family
        var isMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == photo.Post.FamilyId);

        if (!isMember)
            return Forbid();

        var uploads = Path.Combine(_env.ContentRootPath, "uploads");
        var filePath = Path.Combine(uploads, fileName);

        if (!System.IO.File.Exists(filePath))
            return NotFound();

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        string contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };

        return PhysicalFile(filePath, contentType);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePhoto(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var photo = await _context.Photos
            .Include(p => p.Post)
            .ThenInclude(p => p!.Family)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (photo == null || photo.Post == null)
            return NotFound(new { message = "Photo not found" });

        // Check if user is the post author
        var isAuthor = photo.Post.AuthorId == userId;

        // Check if user is a family admin
        var isAdmin = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == photo.Post.FamilyId && fm.Role == "admin");

        if (!isAuthor && !isAdmin)
            return StatusCode(403, new { message = "You don't have permission to delete this photo" });

        // Delete the physical file
        var fileName = Path.GetFileName(photo.Url);
        var uploads = Path.Combine(_env.ContentRootPath, "uploads");
        var filePath = Path.Combine(uploads, fileName);
        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        _context.Photos.Remove(photo);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Photo deleted" });
    }
}
