using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Controllers;

public class MessagesControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MessagesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateAuthenticatedClient(string userId = "msg-user")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            user = new User
            {
                Id = userId,
                Email = $"{userId}@example.com",
                FirstName = "Msg",
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
    public async Task Send_Returns200()
    {
        // Arrange
        var client = CreateAuthenticatedClient("msg-sender");
        var request = new { recipientId = "msg-recipient", familyId = (int?)null, content = "Hello!" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/messages", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Send_SavesMessageToDatabase()
    {
        // Arrange
        var client = CreateAuthenticatedClient("db-msg-sender");
        var request = new { recipientId = "db-msg-recipient", familyId = (int?)null, content = "DB save test" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/api/messages", content);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await context.Messages.FirstAsync();
        Assert.Equal("db-msg-sender", message.SenderId);
        Assert.Equal("db-msg-recipient", message.RecipientId);
        Assert.Equal("DB save test", message.Content);
    }

    [Fact]
    public async Task Send_SavesFamilyMessage()
    {
        // Arrange
        var client = CreateAuthenticatedClient("family-msg-sender");
        var request = new { recipientId = (string?)null, familyId = 1, content = "Family msg" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        await client.PostAsync("/api/messages", content);

        // Assert
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var message = await context.Messages.FirstAsync();
        Assert.Equal(1, message.FamilyId);
    }

    [Fact]
    public async Task GetConversation_Returns200()
    {
        // Arrange
        var client = CreateAuthenticatedClient("conv-user");
        var request = new { recipientId = "conv-recipient", familyId = (int?)null, content = "Hi" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/messages", content);

        // Act
        var response = await client.GetAsync("/api/messages/conversation?recipientId=conv-recipient");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetConversation_ReturnsMessages()
    {
        // Arrange
        var client = CreateAuthenticatedClient("conv-data-sender");
        var request = new { recipientId = "conv-data-recipient", familyId = (int?)null, content = "Conv test msg" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/messages", content);

        // Act
        var response = await client.GetAsync("/api/messages/conversation?recipientId=conv-data-recipient");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = JsonDocument.Parse(body);
        Assert.True(messages.RootElement.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetConversation_ReturnsEmptyArrayForNoMessages()
    {
        // Arrange
        var client = CreateAuthenticatedClient("conv-empty-sender");

        // Act
        var response = await client.GetAsync("/api/messages/conversation?recipientId=conv-empty-recipient");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = JsonDocument.Parse(body);
        Assert.Equal(0, messages.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task GetConversation_ReturnsFamilyMessages()
    {
        // Arrange
        var client = CreateAuthenticatedClient("conv-family-sender");
        var request = new { recipientId = (string?)null, familyId = 1, content = "Family conv msg" };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        await client.PostAsync("/api/messages", content);

        // Act
        var response = await client.GetAsync("/api/messages/conversation?familyId=1");
        var body = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var messages = JsonDocument.Parse(body);
        Assert.True(messages.RootElement.GetArrayLength() > 0);
    }
}
