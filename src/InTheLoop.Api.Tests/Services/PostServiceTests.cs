using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Services;

public class PostServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "InTheLoopPostTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task SetupTestData(ApplicationDbContext context)
    {
        var user = new User { Id = "author-1", Email = "author@example.com", FirstName = "Jane", LastName = "Smith" };
        var family = new Family { Id = 1, Name = "Smith Family", OwnerId = "author-1" };
        context.Users.Add(user);
        context.Families.Add(family);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreatePostAsync_CreatesPostWithCorrectData()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);
        var authorId = "author-1";
        var familyId = 1;
        var content = "Hello from the feed!";

        // Act
        var result = await service.CreatePostAsync(authorId, familyId, content);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(content, result.Content);
        Assert.Equal("Jane Smith", result.AuthorName);
        Assert.Equal(familyId, result.FamilyId);
        Assert.Equal("Smith Family", result.FamilyName);
        Assert.NotNull(result.CreatedAt);
        Assert.True(result.PhotoUrls.Count == 0);
    }

    [Fact]
    public async Task CreatePostAsync_SetsCreatedAtTimestamp()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Act
        var result = await service.CreatePostAsync("author-1", 1, "Timestamp test");

        // Assert
        Assert.True(result.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(result.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetFamilyFeedAsync_ReturnsPostsInDescendingOrder()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Create posts in a specific order
        var post1 = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "First post", CreatedAt = DateTime.UtcNow.AddHours(-3) };
        var post2 = new Post { Id = 2, AuthorId = "author-1", FamilyId = 1, Content = "Second post", CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var post3 = new Post { Id = 3, AuthorId = "author-1", FamilyId = 1, Content = "Third post", CreatedAt = DateTime.UtcNow };

        context.Posts.AddRange(post1, post2, post3);
        await context.SaveChangesAsync();

        // Act
        var feed = await service.GetFamilyFeedAsync(1);
        var feedList = feed.ToList();

        // Assert
        Assert.Equal(3, feedList.Count);
        Assert.Equal("Third post", feedList[0].Content);
        Assert.Equal("Second post", feedList[1].Content);
        Assert.Equal("First post", feedList[2].Content);
    }

    [Fact]
    public async Task GetFamilyFeedAsync_RespectsSkipAndTake()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Create 5 posts
        for (int i = 1; i <= 5; i++)
        {
            context.Posts.Add(new Post
            {
                Id = i,
                AuthorId = "author-1",
                FamilyId = 1,
                Content = $"Post {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act - skip 2, take 2 (should get posts 4 and 5, which are newest)
        var feed = await service.GetFamilyFeedAsync(1, skip: 2, take: 2);
        var feedList = feed.ToList();

        // Assert
        Assert.Equal(2, feedList.Count);
        // Posts ordered by CreatedAt desc: 5,4,3,2,1. Skip 2 = [3,2,1], Take 2 = [3,2]
        Assert.Equal(2, feedList.Count);
        Assert.Contains(feedList, f => f.Content == "Post 3");
        Assert.Contains(feedList, f => f.Content == "Post 2");
    }

    [Fact]
    public async Task GetFamilyFeedAsync_ReturnsEmptyForUnknownFamily()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Act
        var feed = await service.GetFamilyFeedAsync(999);

        // Assert
        Assert.Empty(feed);
    }

    [Fact]
    public async Task GetFamilyFeedAsync_IncludesAuthorName()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Act
        await service.CreatePostAsync("author-1", 1, "Test content");
        var feed = await service.GetFamilyFeedAsync(1);
        var post = feed.First();

        // Assert
        Assert.Equal("Jane Smith", post.AuthorName);
    }

    [Fact]
    public async Task GetFamilyFeedAsync_IncludesPhotoUrls()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Add a photo
        var post = new Post { Id = 1, AuthorId = "author-1", FamilyId = 1, Content = "With photo", CreatedAt = DateTime.UtcNow };
        var photo = new Photo { Id = 1, PostId = 1, Url = "/uploads/test.jpg" };
        context.Posts.Add(post);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Act
        var feed = await service.GetFamilyFeedAsync(1);
        var postDto = feed.First();

        // Assert
        Assert.Equal(1, postDto.PhotoUrls.Count);
        Assert.Equal("/uploads/test.jpg", postDto.PhotoUrls[0]);
    }

    [Fact]
    public async Task GetFamilyFeedAsync_DefaultTakeIs20()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Create 25 posts
        for (int i = 1; i <= 25; i++)
        {
            context.Posts.Add(new Post
            {
                Id = i,
                AuthorId = "author-1",
                FamilyId = 1,
                Content = $"Post {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Act
        var feed = await service.GetFamilyFeedAsync(1); // default take=20

        // Assert
        Assert.Equal(20, feed.Count());
    }

    [Fact]
    public async Task GetFamilyFeedAsync_AuthorAvatarIsNullWhenNotSet()
    {
        // Arrange
        var context = CreateTestContext();
        await SetupTestData(context);
        var service = new PostService(context);

        // Act
        await service.CreatePostAsync("author-1", 1, "Test");
        var feed = await service.GetFamilyFeedAsync(1);
        var post = feed.First();

        // Assert
        Assert.Null(post.AuthorAvatar);
    }
}
