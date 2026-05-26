using InTheLoop.Api.Data;
using InTheLoop.Api.DTOs;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class PhotoServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "PhotoTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private TestWebHostEnvironment CreateTestEnvironment()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "InTheLoopPhotoTest_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        Directory.CreateDirectory(Path.Combine(tempDir, "photos"));
        Directory.CreateDirectory(Path.Combine(tempDir, "wwwroot"));
        return new TestWebHostEnvironment(tempDir);
    }

    private class TestWebHostEnvironment : IWebHostEnvironment
    {
        public TestWebHostEnvironment(string contentRoot)
        {
            ContentRootPath = contentRoot;
            WebRootPath = Path.Combine(contentRoot, "wwwroot");
            WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
        }

        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "InTheLoop.Api.Tests";
    }

    [Fact]
    public async Task GetPostPhotosAsync_ReturnsPhotosForPost()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "Test" };
        var photo1 = new Photo { Id = 1, PostId = 1, Url = "/uploads/photo1.jpg" };
        var photo2 = new Photo { Id = 2, PostId = 1, Url = "/uploads/photo2.jpg" };

        context.Posts.Add(post);
        context.Photos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        // Act
        var photos = await service.GetPostPhotosAsync(1);

        // Assert
        Assert.Equal(2, photos.Count);
        Assert.Contains(photos, p => p.Url == "/uploads/photo1.jpg");
        Assert.Contains(photos, p => p.Url == "/uploads/photo2.jpg");
    }

    [Fact]
    public async Task GetPostPhotosAsync_ReturnsEmptyForPostWithNoPhotos()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "No photos" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var photos = await service.GetPostPhotosAsync(1);

        // Assert
        Assert.Empty(photos);
    }

    [Fact]
    public async Task GetPostPhotosAsync_ReturnsEmptyForUnknownPost()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        // Act
        var photos = await service.GetPostPhotosAsync(999);

        // Assert
        Assert.Empty(photos);
    }

    [Fact]
    public async Task GetPostPhotosAsync_SetsUploadedAtTimestamp()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "Test" };
        var photo = new Photo { Id = 1, PostId = 1, Url = "/uploads/test.jpg" };
        context.Posts.Add(post);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Act
        var photos = await service.GetPostPhotosAsync(1);
        var uploadedAt = photos.First().UploadedAt;

        // Assert
        Assert.True(uploadedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task GetPostPhotosAsync_SetsAltText()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "Alt text test" };
        var photo = new Photo { Id = 1, PostId = 1, Url = "/uploads/alt.jpg", AltText = "Test alt" };
        context.Posts.Add(post);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Act
        var photos = await service.GetPostPhotosAsync(1);

        // Assert
        Assert.Equal("Test alt", photos.First().AltText);
    }

    [Fact]
    public async Task GetPostPhotosAsync_ReturnsIListType()
    {
        // Arrange
        var context = CreateTestContext();
        var env = CreateTestEnvironment();
        var service = new PhotoService(context, env);

        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "Type test" };
        var photo = new Photo { Id = 1, PostId = 1, Url = "/uploads/type.jpg" };
        context.Posts.Add(post);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Act
        var photos = await service.GetPostPhotosAsync(1);

        // Assert
        Assert.IsAssignableFrom<System.Collections.Generic.IList<Photo>>(photos);
    }
}
