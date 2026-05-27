using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Tests.Controllers;

public class CommentsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public CommentsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId = "test-user-1")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Create the user first
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = $"{userId}@example.com",
                FirstName = "Test",
                LastName = "User"
            };
            context.Users.Add(user);
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
    public async Task AddComment_Returns200WithCommentData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("comment-add-user");
        var familyClient = CreateAuthenticatedClient("comment-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Comment Family", description = "Comment test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        // Create a post first
        var postClient = CreateAuthenticatedClient("comment-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "Test post for comments" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Act
        var commentRequest = new { content = "This is a test comment" };
        var response = await PostJsonAsync(client, $"/api/comments/posts/{postId}", commentRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("This is a test comment", json.RootElement.GetProperty("content").GetString());
        Assert.Equal("comment-add-user", json.RootElement.GetProperty("authorId").GetString());
        Assert.Equal(postId, json.RootElement.GetProperty("postId").GetInt32());
    }

    [Fact]
    public async Task AddComment_CreatesCommentInDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("comment-db-user");
        var familyClient = CreateAuthenticatedClient("comment-db-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Comment DB Family", description = "DB comment test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        var postClient = CreateAuthenticatedClient("comment-db-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "DB post for comments" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Act
        var commentRequest = new { content = "DB test comment" };
        await PostJsonAsync(client, $"/api/comments/posts/{postId}", commentRequest);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var comment = await context.Comments.FirstAsync(c => c.PostId == postId && c.Content == "DB test comment");
        Assert.NotNull(comment);
        Assert.Equal("comment-db-user", comment.AuthorId);
    }

    [Fact]
    public async Task GetComments_Returns200WithComments()
    {
        // Arrange
        var client = CreateAuthenticatedClient("comment-get-user");
        var familyClient = CreateAuthenticatedClient("comment-get-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Get Comments Family", description = "Get comments test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        var postClient = CreateAuthenticatedClient("comment-get-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "Post for getting comments" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Add some comments
        var commentClient = CreateAuthenticatedClient("comment-adder");
        await PostJsonAsync(commentClient, $"/api/comments/posts/{postId}", new { content = "Comment 1" });
        await PostJsonAsync(commentClient, $"/api/comments/posts/{postId}", new { content = "Comment 2" });

        // Act
        var response = await client.GetAsync($"/api/comments/posts/{postId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Comment 1", body);
        Assert.Contains("Comment 2", body);
    }

    [Fact]
    public async Task AddReply_Returns200WithReplyData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("reply-user");
        var familyClient = CreateAuthenticatedClient("reply-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Reply Family", description = "Reply test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        var postClient = CreateAuthenticatedClient("reply-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "Post for replies" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Add a parent comment first
        var commentClient = CreateAuthenticatedClient("comment-author");
        var commentResponse = await PostJsonAsync(commentClient, $"/api/comments/posts/{postId}", new { content = "Parent comment" });
        var commentBody = await commentResponse.Content.ReadAsStringAsync();
        var commentJson = JsonDocument.Parse(commentBody);
        var commentId = commentJson.RootElement.GetProperty("id").GetInt32();

        // Act
        var replyRequest = new { postId = postId, content = "This is a reply" };
        var response = await PostJsonAsync(client, $"/api/comments/{commentId}/reply", replyRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("This is a reply", json.RootElement.GetProperty("content").GetString());
        Assert.Equal(commentId, json.RootElement.GetProperty("parentCommentId").GetInt32());
    }

    [Fact]
    public async Task AddReply_CreatesNestedReplyInDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("reply-db-user");
        var familyClient = CreateAuthenticatedClient("reply-db-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Reply DB Family", description = "DB reply test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        var postClient = CreateAuthenticatedClient("reply-db-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "DB post for replies" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Add a parent comment first
        var commentClient = CreateAuthenticatedClient("reply-db-author");
        var commentResponse = await PostJsonAsync(commentClient, $"/api/comments/posts/{postId}", new { content = "DB parent comment" });
        var commentBody = await commentResponse.Content.ReadAsStringAsync();
        var commentJson = JsonDocument.Parse(commentBody);
        var commentId = commentJson.RootElement.GetProperty("id").GetInt32();

        // Act
        var replyRequest = new { postId = postId, content = "DB reply" };
        await PostJsonAsync(client, $"/api/comments/{commentId}/reply", replyRequest);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var reply = await context.Comments.FirstAsync(c => c.ParentCommentId == commentId && c.Content == "DB reply");
        Assert.NotNull(reply);
        Assert.Equal("reply-db-user", reply.AuthorId);
    }

    [Fact]
    public async Task GetComments_ReturnsEmptyListWhenNoComments()
    {
        // Arrange
        var client = CreateAuthenticatedClient("empty-comments-user");
        var familyClient = CreateAuthenticatedClient("empty-comments-family-owner");
        var familyResponse = await PostJsonAsync(familyClient, "/api/families", new { name = "Empty Comments Family", description = "Empty comments test" });
        var familyBody = await familyResponse.Content.ReadAsStringAsync();
        var familyJson = JsonDocument.Parse(familyBody);
        var familyId = familyJson.RootElement.GetProperty("id").GetInt32();

        var postClient = CreateAuthenticatedClient("empty-comments-post-user");
        var postResponse = await PostJsonAsync(postClient, "/api/posts", new { familyId = familyId, content = "Post with no comments" });
        var postBody = await postResponse.Content.ReadAsStringAsync();
        var postJson = JsonDocument.Parse(postBody);
        var postId = postJson.RootElement.GetProperty("id").GetInt32();

        // Act
        var response = await client.GetAsync($"/api/comments/posts/{postId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal(0, json.RootElement.GetArrayLength());
    }
}
