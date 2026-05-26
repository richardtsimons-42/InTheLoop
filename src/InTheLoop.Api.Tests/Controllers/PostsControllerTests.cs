using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Tests.Controllers;

public class PostsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public PostsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int GetFamilyIdForUser(string userId)
    {
        var hash = userId.GetHashCode();
        return (int)((uint)(hash ^ (hash >> 16)) % 100000 + 1);
    }

    private HttpClient CreateAuthenticatedClient(string userId = "post-user-1")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Create the user and a family first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = $"{userId}@example.com",
                FirstName = "Post",
                LastName = "User"
            };
            context.Users.Add(user);
            context.SaveChanges();
        }

        var familyId = GetFamilyIdForUser(userId);

        var family = context.Families.FirstOrDefault(f => f.OwnerId == userId);
        if (family == null)
        {
            family = new Family { Id = familyId, Name = "Post Family", OwnerId = userId };
            context.Families.Add(family);
            context.FamilyMembers.Add(new FamilyMember
            {
                UserId = userId,
                FamilyId = familyId,
                Role = "admin"
            });
            context.SaveChanges();
        }

        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        return client;
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object value)
    {
        var json = JsonSerializer.Serialize(value);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    [Fact]
    public async Task CreatePost_Returns200WithPostData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("create-post-user");
        var familyId = GetFamilyIdForUser("create-post-user");
        var request = new { familyId, content = "Hello world!" };

        // Act
        var response = await PostJsonAsync(client, "/api/posts", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("Hello world!", json.RootElement.GetProperty("content").GetString());
        Assert.True(json.RootElement.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreatePost_CreatesPostInDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("db-post-user");
        var familyId = GetFamilyIdForUser("db-post-user");
        var request = new { familyId, content = "Database post" };

        // Act
        await PostJsonAsync(client, "/api/posts", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var post = await context.Posts.FirstAsync(p => p.Content == "Database post");
        Assert.NotNull(post);
        Assert.Equal("db-post-user", post.AuthorId);
        Assert.Equal(familyId, post.FamilyId);
    }

    [Fact]
    public async Task CreatePost_SetsCreatedAt()
    {
        // Arrange
        var client = CreateAuthenticatedClient("timestamp-post-user");
        var familyId = GetFamilyIdForUser("timestamp-post-user");
        var request = new { familyId, content = "Timestamp post" };

        // Act
        await PostJsonAsync(client, "/api/posts", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var post = await context.Posts.FirstAsync(p => p.Content == "Timestamp post");
        Assert.True(post.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(post.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task CreatePost_SetsAuthorName()
    {
        // Arrange
        var client = CreateAuthenticatedClient("author-name-user");
        var familyId = GetFamilyIdForUser("author-name-user");
        var request = new { familyId, content = "Author test" };

        // Act
        var response = await PostJsonAsync(client, "/api/posts", request);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var authorName = json.RootElement.GetProperty("authorName").GetString();

        // Assert
        Assert.Equal("Post User", authorName);
    }

    [Fact]
    public async Task CreatePost_SetsFamilyName()
    {
        // Arrange
        var client = CreateAuthenticatedClient("family-name-user");
        var familyId = GetFamilyIdForUser("family-name-user");
        var request = new { familyId, content = "Family name test" };

        // Act
        var response = await PostJsonAsync(client, "/api/posts", request);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var familyName = json.RootElement.GetProperty("familyName").GetString();

        // Assert
        Assert.Equal("Post Family", familyName);
    }

    [Fact]
    public async Task GetFeed_Returns200WithPosts()
    {
        // Arrange
        var client = CreateAuthenticatedClient("feed-user");
        var familyId = GetFamilyIdForUser("feed-user");
        var postRequest = new { familyId, content = "Feed post 1" };
        await PostJsonAsync(client, "/api/posts", postRequest);

        // Act
        var response = await client.GetAsync($"/api/posts/{familyId}/feed");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Feed post 1", body);
    }

    [Fact]
    public async Task GetFeed_ReturnsPostsInDescendingOrder()
    {
        // Arrange
        var client = CreateAuthenticatedClient("order-feed-user");
        var familyId = GetFamilyIdForUser("order-feed-user");

        // Create posts in a specific order
        var post1 = new { familyId, content = "First post" };
        var post2 = new { familyId, content = "Second post" };
        var post3 = new { familyId, content = "Third post" };

        await PostJsonAsync(client, "/api/posts", post1);
        await Task.Delay(10);
        await PostJsonAsync(client, "/api/posts", post2);
        await Task.Delay(10);
        await PostJsonAsync(client, "/api/posts", post3);

        // Act
        var response = await client.GetAsync($"/api/posts/{familyId}/feed");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(body);
        var firstContent = json.RootElement[0].GetProperty("content").GetString();
        Assert.Equal("Third post", firstContent);
    }

    [Fact]
    public async Task GetFeed_RespectsSkipAndTake()
    {
        // Arrange
        var client = CreateAuthenticatedClient("pagination-user");
        var familyId = GetFamilyIdForUser("pagination-user");

        // Create 5 posts
        for (int i = 1; i <= 5; i++)
        {
            await PostJsonAsync(client, "/api/posts", new { familyId, content = $"Post {i}" });
        }

        // Act - skip 2, take 2
        var response = await client.GetAsync($"/api/posts/{familyId}/feed?skip=2&take=2");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetFeed_ReturnsEmptyForFamilyWithNoPosts()
    {
        // Arrange
        var client = CreateAuthenticatedClient("empty-feed-user");
        var familyId = GetFamilyIdForUser("empty-feed-user");

        // Act
        var response = await client.GetAsync($"/api/posts/{familyId}/feed");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", body.Trim());
    }

    [Fact]
    public async Task GetFeed_DefaultTakeIs20()
    {
        // Arrange
        var client = CreateAuthenticatedClient("default-take-user");
        var familyId = GetFamilyIdForUser("default-take-user");

        // Create 25 posts
        for (int i = 1; i <= 25; i++)
        {
            await PostJsonAsync(client, "/api/posts", new { familyId, content = $"Post {i}" });
        }

        // Act
        var response = await client.GetAsync($"/api/posts/{familyId}/feed");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(20, json.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task CreatePost_SetsPhotoUrlsToEmptyList()
    {
        // Arrange
        var client = CreateAuthenticatedClient("photo-urls-user");
        var familyId = GetFamilyIdForUser("photo-urls-user");
        var request = new { familyId, content = "No photos" };

        // Act
        var response = await PostJsonAsync(client, "/api/posts", request);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var photoUrls = json.RootElement.GetProperty("photoUrls");

        // Assert
        Assert.True(photoUrls.GetArrayLength() == 0);
    }

    [Fact]
    public async Task CreatePost_UsesCorrectFamilyId()
    {
        // Arrange
        var client = CreateAuthenticatedClient("correct-family-user");
        var familyId = GetFamilyIdForUser("correct-family-user");
        var request = new { familyId, content = "Correct family test" };

        // Act
        await PostJsonAsync(client, "/api/posts", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var post = await context.Posts.FirstAsync(p => p.Content == "Correct family test");
        Assert.Equal(familyId, post.FamilyId);
    }
}
