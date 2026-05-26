using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace Tests.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string url, object value)
    {
        var json = JsonSerializer.Serialize(value);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
        return await client.PostAsync(url, content);
    }

    [Fact]
    public async Task Register_Returns200WithToken()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "test@example.com", password = "TestPass123!", firstName = "Test", lastName = "User" };

        // Act
        var response = await PostJsonAsync(client, "/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("token", out var tokenProp));
        Assert.True(tokenProp.GetString()?.Length > 0);
    }

    [Fact]
    public async Task Register_CreatesUserInDatabase()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "dbuser@example.com", password = "TestPass123!", firstName = "DB", lastName = "User" };

        // Act
        await PostJsonAsync(client, "/api/auth/register", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == "dbuser@example.com");
        Assert.NotNull(user);
        Assert.Equal("DB", user.FirstName);
        Assert.Equal("User", user.LastName);
        Assert.False(user.IsVerified);
    }

    [Fact]
    public async Task Register_SetsCreatedAtTimestamp()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "time@example.com", password = "TestPass123!", firstName = "Time", lastName = "User" };

        // Act
        await PostJsonAsync(client, "/api/auth/register", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == "time@example.com");
        Assert.True(user.CreatedAt > DateTime.UnixEpoch);
        Assert.True(user.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Register_SetsIsVerifiedToFalse()
    {
        // Arrange
        var client = CreateClient();
        var request = new { email = "unverified@example.com", password = "TestPass123!", firstName = "Unverified", lastName = "User" };

        // Act
        await PostJsonAsync(client, "/api/auth/register", request);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await context.Users.FirstAsync(u => u.Email == "unverified@example.com");
        Assert.False(user.IsVerified);
    }

    [Fact]
    public async Task Login_Returns200WithValidCredentials()
    {
        // Arrange
        var registerClient = CreateClient();
        var registerRequest = new { email = "login@example.com", password = "TestPass123!", firstName = "Login", lastName = "User" };
        await PostJsonAsync(registerClient, "/api/auth/register", registerRequest);

        var client = CreateClient();
        var loginRequest = new { email = "login@example.com", password = "TestPass123!" };

        // Act
        var response = await PostJsonAsync(client, "/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("token", out _));
    }

    [Fact]
    public async Task Login_Returns401WithInvalidPassword()
    {
        // Arrange
        var registerClient = CreateClient();
        var registerRequest = new { email = "wrongpass@example.com", password = "CorrectPass123!", firstName = "Wrong", lastName = "Pass" };
        await PostJsonAsync(registerClient, "/api/auth/register", registerRequest);

        var client = CreateClient();
        var loginRequest = new { email = "wrongpass@example.com", password = "WrongPassword123!" };

        // Act
        var response = await PostJsonAsync(client, "/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_Returns401WithNonExistentUser()
    {
        // Arrange
        var client = CreateClient();
        var loginRequest = new { email = "nonexistent@example.com", password = "AnyPass123!" };

        // Act
        var response = await PostJsonAsync(client, "/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsTokenWithCorrectClaims()
    {
        // Arrange
        var registerClient = CreateClient();
        var registerRequest = new { email = "claims@example.com", password = "TestPass123!", firstName = "Claims", lastName = "User" };
        await PostJsonAsync(registerClient, "/api/auth/register", registerRequest);

        var client = CreateClient();
        var loginRequest = new { email = "claims@example.com", password = "TestPass123!" };

        // Act
        var response = await PostJsonAsync(client, "/api/auth/login", loginRequest);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        var token = json.RootElement.GetProperty("token").GetString();

        // Assert
        Assert.NotNull(token);
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);
        var payload = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=')));
        Assert.Contains("claims@example.com", payload);
    }

    [Fact]
    public async Task Logout_Returns200WhenAuthenticated()
    {
        // Arrange
        var registerClient = CreateClient();
        var registerRequest = new { email = "logout@example.com", password = "TestPass123!", firstName = "Logout", lastName = "User" };
        await PostJsonAsync(registerClient, "/api/auth/register", registerRequest);

        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "test-user-logout");
        // Create the user in the test DB so the auth handler can find it
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = "test-user-logout",
            UserName = "logout@example.com",
            Email = "logout@example.com",
            FirstName = "Logout",
            LastName = "User",
            IsVerified = false
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task Logout_Returns401WhenNotAuthenticated()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_Returns401WithInvalidTestUser()
    {
        // Arrange
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-User-Id", "nonexistent-user");

        // Act
        var response = await client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
