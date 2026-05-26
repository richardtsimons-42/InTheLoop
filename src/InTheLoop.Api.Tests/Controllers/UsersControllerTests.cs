using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers;

public class UsersControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public UsersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId = "profile-user")
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
                FirstName = "Profile",
                LastName = "User"
            };
            context.Users.Add(user);
            context.SaveChanges();
        }

        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        return client;
    }

    [Fact]
    public async Task GetMyProfile_Returns200WithUserData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("get-profile-user");

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("get-profile-user", json.RootElement.GetProperty("id").GetString());
        Assert.Equal("get-profile-user@example.com", json.RootElement.GetProperty("email").GetString());
        Assert.Equal("Profile", json.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("User", json.RootElement.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task GetMyProfile_ReturnsCreatedAt()
    {
        // Arrange
        var client = CreateAuthenticatedClient("created-at-profile-user");

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("createdAt", out _));
    }

    [Fact]
    public async Task GetMyProfile_ReturnsNullAvatarWhenNoneSet()
    {
        // Arrange
        var client = CreateAuthenticatedClient("no-avatar-user");

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("avatarUrl", out var avatarProp));
        Assert.True(avatarProp.ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task UpdateMyProfile_Returns200WithUpdatedData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("update-profile-user");
        var request = new { firstName = "Updated", lastName = "Name" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        Assert.Equal("Updated", parsed.RootElement.GetProperty("firstName").GetString());
        Assert.Equal("Name", parsed.RootElement.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task UpdateMyProfile_PersistsToDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("db-update-user");
        var request = new { firstName = "DB", lastName = "Updated" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        await client.PutAsync("/api/users/me", content);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstAsync(u => u.Id == "db-update-user");
        Assert.Equal("DB", user.FirstName);
        Assert.Equal("Updated", user.LastName);
    }

    [Fact]
    public async Task UpdateMyProfile_Returns400WhenFirstNameEmpty()
    {
        // Arrange
        var client = CreateAuthenticatedClient("empty-name-user");
        var request = new { firstName = "", lastName = "Name" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_Returns400WhenLastNameEmpty()
    {
        // Arrange
        var client = CreateAuthenticatedClient("empty-lastname-user");
        var request = new { firstName = "First", lastName = "" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAvatar_Returns200WithUpdatedAvatar()
    {
        // Arrange
        var client = CreateAuthenticatedClient("avatar-user");
        var request = new { avatarUrl = "https://example.com/new-avatar.png" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me/avatar", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        Assert.Equal("https://example.com/new-avatar.png", parsed.RootElement.GetProperty("avatarUrl").GetString());
    }

    [Fact]
    public async Task UpdateAvatar_PersistsToDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("db-avatar-user");
        var request = new { avatarUrl = "https://example.com/db-avatar.png" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        await client.PutAsync("/api/users/me/avatar", content);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstAsync(u => u.Id == "db-avatar-user");
        Assert.Equal("https://example.com/db-avatar.png", user.AvatarUrl);
    }

    [Fact]
    public async Task UpdateAvatar_Returns400WhenUrlEmpty()
    {
        // Arrange
        var client = CreateAuthenticatedClient("empty-avatar-user");
        var request = new { avatarUrl = "" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me/avatar", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMyProfile_PreservesEmail()
    {
        // Arrange
        var client = CreateAuthenticatedClient("email-preserve-user");
        var request = new { firstName = "Changed", lastName = "Name" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PutAsync("/api/users/me", content);

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        Assert.Equal("email-preserve-user@example.com", parsed.RootElement.GetProperty("email").GetString());
    }

    [Fact]
    public async Task GetMyProfile_ReturnsVerifiedStatus()
    {
        // Arrange
        var client = CreateAuthenticatedClient("verified-user");
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = context.Users.First(u => u.Id == "verified-user");
        user.IsVerified = true;
        context.SaveChanges();

        // Act
        var response = await client.GetAsync("/api/users/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var parsed = JsonDocument.Parse(body);
        Assert.True(parsed.RootElement.GetProperty("isVerified").GetBoolean());
    }
}
