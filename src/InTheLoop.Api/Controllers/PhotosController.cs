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
        var photos = await _context.Photos
            .Where(p => p.PostId == postId)
            .OrderBy(p => p.UploadedAt)
            .ToListAsync();

        return Ok(photos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPhoto(int id)
    {
        var photo = await _context.Photos.FindAsync(id);
        if (photo == null)
            return NotFound(new { message = "Photo not found" });

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
    public IActionResult GetPhotoFile(string fileName)
    {
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
        var photo = await _context.Photos.FindAsync(id);
        if (photo == null)
            return NotFound(new { message = "Photo not found" });

        _context.Photos.Remove(photo);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Photo deleted" });
    }
}
