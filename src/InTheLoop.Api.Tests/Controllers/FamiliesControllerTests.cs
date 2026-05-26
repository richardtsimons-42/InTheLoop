using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Tests.Controllers;

public class FamiliesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public FamiliesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static int GetFamilyIdForUser(string userId)
    {
        var hash = userId.GetHashCode();
        return (int)((uint)(hash ^ (hash >> 16)) % 100000 + 1);
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

    private static async Task<HttpResponseMessage> PostJsonNullAsync(HttpClient client, string url)
    {
        var content = new StringContent("null", Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    [Fact]
    public async Task CreateFamily_Returns200WithFamilyData()
    {
        // Arrange
        var client = CreateAuthenticatedClient("create-family-user");
        var request = new { name = "Test Family", description = "A test family" };

        // Act
        var response = await PostJsonAsync(client, "/api/families", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.Equal("Test Family", json.RootElement.GetProperty("name").GetString());
        Assert.Equal("A test family", json.RootElement.GetProperty("description").GetString());
    }

    [Fact]
    public async Task CreateFamily_CreatesFamilyInDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("db-create-family-user");
        var request = new { name = "DB Family", description = "Database test" };

        // Act
        await PostJsonAsync(client, "/api/families", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await context.Families.FirstAsync(f => f.Name == "DB Family");
        Assert.NotNull(family);
        Assert.Equal("DB Family", family.Name);
        Assert.Equal("db-create-family-user", family.OwnerId);
    }

    [Fact]
    public async Task CreateFamily_CreatesOwnerAsAdmin()
    {
        // Arrange
        var client = CreateAuthenticatedClient("admin-member-user");
        var request = new { name = "Admin Family", description = "Admin test" };

        // Act
        await PostJsonAsync(client, "/api/families", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var member = await context.FamilyMembers.FirstAsync(m => m.UserId == "admin-member-user");
        Assert.Equal("admin", member.Role);
    }

    [Fact]
    public async Task CreateFamily_SetsCreatedAt()
    {
        // Arrange
        var client = CreateAuthenticatedClient("timestamp-user");
        var request = new { name = "Time Family", description = "Timestamp test" };

        // Act
        await PostJsonAsync(client, "/api/families", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var family = await context.Families.FirstAsync(f => f.Name == "Time Family");
        Assert.True(family.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(family.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetUserFamilies_Returns200WithFamilies()
    {
        // Arrange
        var client = CreateAuthenticatedClient("get-families-user");
        var request = new { name = "My Family", description = "My family" };
        await PostJsonAsync(client, "/api/families", request);

        // Act
        var response = await client.GetAsync("/api/families");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("My Family", body);
    }

    [Fact]
    public async Task GetUserFamilies_ReturnsMultipleFamilies()
    {
        // Arrange
        var client = CreateAuthenticatedClient("multi-family-user");
        await PostJsonAsync(client, "/api/families", new { name = "Family 1", description = "First" });
        await PostJsonAsync(client, "/api/families", new { name = "Family 2", description = "Second" });

        // Act
        var response = await client.GetAsync("/api/families");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Family 1", body);
        Assert.Contains("Family 2", body);
    }

    [Fact]
    public async Task JoinFamily_Returns200()
    {
        // Arrange
        var ownerClient = CreateAuthenticatedClient("join-owner");
        var createResponse = await PostJsonAsync(ownerClient, "/api/families", new { name = "Join Family", description = "Join test" });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createBody);
        var familyId = createJson.RootElement.GetProperty("id").GetInt32();

        var memberClient = CreateAuthenticatedClient("join-member");

        // Act
        var response = await PostJsonNullAsync(memberClient, $"/api/families/{familyId}/join");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(JsonDocument.Parse(body).RootElement.GetBoolean());
    }

    [Fact]
    public async Task JoinFamily_CreatesMemberInDatabase()
    {
        // Arrange
        var ownerClient = CreateAuthenticatedClient("join-db-owner");
        var createResponse = await PostJsonAsync(ownerClient, "/api/families", new { name = "Join DB Family", description = "DB test" });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createBody);
        var familyId = createJson.RootElement.GetProperty("id").GetInt32();

        var memberClient = CreateAuthenticatedClient("join-db-member");

        // Act
        await PostJsonNullAsync(memberClient, $"/api/families/{familyId}/join");

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var member = await context.FamilyMembers.FirstAsync(m => m.UserId == "join-db-member");
        Assert.Equal("member", member.Role);
    }

    [Fact]
    public async Task JoinFamily_ReturnsFalseWhenAlreadyMember()
    {
        // Arrange
        var ownerClient = CreateAuthenticatedClient("double-join-owner");
        var createResponse = await PostJsonAsync(ownerClient, "/api/families", new { name = "Double Join Family", description = "Double test" });
        var createBody = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createBody);
        var familyId = createJson.RootElement.GetProperty("id").GetInt32();

        var memberClient = CreateAuthenticatedClient("double-join-member");
        await PostJsonNullAsync(memberClient, $"/api/families/{familyId}/join");

        // Act (try to join again)
        var response = await PostJsonNullAsync(memberClient, $"/api/families/{familyId}/join");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(JsonDocument.Parse(body).RootElement.GetBoolean());
    }

    [Fact]
    public async Task CreateFamily_SetsMemberCountToOne()
    {
        // Arrange
        var client = CreateAuthenticatedClient("member-count-user");
        var request = new { name = "Count Family", description = "Member count test" };

        // Act
        var response = await PostJsonAsync(client, "/api/families", request);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var memberCount = json.RootElement.GetProperty("memberCount").GetInt32();

        // Assert
        Assert.Equal(1, memberCount);
    }
}
