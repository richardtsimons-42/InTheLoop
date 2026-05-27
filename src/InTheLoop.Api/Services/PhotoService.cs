using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class PhotoService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PhotoService(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<string> UploadPhotoAsync(int postId, IFormFile file)
    {
        var uploads = Path.Combine(_env.ContentRootPath, "uploads");
        Directory.CreateDirectory(uploads);

        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploads, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        var photo = new Photo
        {
            PostId = postId,
            Url = $"/api/photos/file/{fileName}"
        };

        _context.Photos.Add(photo);
        await _context.SaveChangesAsync();

        return photo.Url;
    }

    public async Task<IList<Photo>> GetPostPhotosAsync(int postId)
    {
        return await _context.Photos
            .Where(p => p.PostId == postId)
            .ToListAsync();
    }
}
