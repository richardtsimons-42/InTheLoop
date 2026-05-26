using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class MessageServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "MessageTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SendMessageAsync_SavesMessageToDatabase()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        await service.SendMessageAsync("sender-1", "recipient-1", null, "Hello!");

        // Assert
        var message = await context.Messages.FirstAsync();
        Assert.Equal("sender-1", message.SenderId);
        Assert.Equal("recipient-1", message.RecipientId);
        Assert.Null(message.FamilyId);
        Assert.Equal("Hello!", message.Content);
    }

    [Fact]
    public async Task SendMessageAsync_SavesFamilyMessage()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        await service.SendMessageAsync("sender-1", null, 1, "Family message");

        // Assert
        var message = await context.Messages.FirstAsync();
        Assert.Equal("sender-1", message.SenderId);
        Assert.Equal(1, message.FamilyId);
        Assert.Equal("Family message", message.Content);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsDirectMessagesBetweenUsers()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        await service.SendMessageAsync("user-a", "user-b", null, "Hi from A");
        await service.SendMessageAsync("user-b", "user-a", null, "Hi from B");
        await service.SendMessageAsync("user-a", "user-b", null, "Another from A");

        // Act
        var messages = await service.GetConversationAsync("user-a", "user-b", null);

        // Assert
        var list = messages.ToList();
        Assert.Equal(3, list.Count);
        Assert.Equal("Hi from A", list[0].Content);
        Assert.Equal("Hi from B", list[1].Content);
        Assert.Equal("Another from A", list[2].Content);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsFamilyMessages()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        await service.SendMessageAsync("sender-1", null, 1, "Family msg 1");
        await service.SendMessageAsync("sender-2", null, 1, "Family msg 2");

        // Act
        var messages = await service.GetConversationAsync("sender-1", null, 1);

        // Assert
        var list = messages.ToList();
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetConversationAsync_OrdersBySentAt()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        var oldTime = DateTime.UtcNow.AddHours(-1);
        var newTime = DateTime.UtcNow;

        var msg1 = new Message { SenderId = "s1", RecipientId = "r1", Content = "Old", SentAt = oldTime };
        var msg2 = new Message { SenderId = "s1", RecipientId = "r1", Content = "New", SentAt = newTime };
        context.Messages.AddRange(msg1, msg2);
        await context.SaveChangesAsync();

        // Act
        var messages = await service.GetConversationAsync("s1", "r1", null);

        // Assert
        var list = messages.ToList();
        Assert.Equal("Old", list[0].Content);
        Assert.Equal("New", list[1].Content);
    }

    [Fact]
    public async Task GetConversationAsync_LimitsTo100Messages()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        for (int i = 0; i < 150; i++)
        {
            await service.SendMessageAsync("s1", "r1", null, $"Message {i}");
        }

        // Act
        var messages = await service.GetConversationAsync("s1", "r1", null);

        // Assert
        var list = messages.ToList();
        Assert.Equal(100, list.Count);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsEmptyForNoMessages()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        var messages = await service.GetConversationAsync("nonexistent", "also-nonexistent", null);

        // Assert
        Assert.Empty(messages);
    }

    [Fact]
    public async Task GetConversationAsync_IncludesSenderData()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        var user = new User
        {
            Id = "sender-with-data",
            Email = "sender@example.com",
            FirstName = "Sender",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        await service.SendMessageAsync("sender-with-data", "recipient", null, "With sender data");

        // Act
        var messages = await service.GetConversationAsync("sender-with-data", "recipient", null);

        // Assert
        var list = messages.ToList();
        Assert.Single(list);
        Assert.NotNull(list[0].Sender);
        Assert.Equal("Sender", list[0].Sender.FirstName);
        Assert.Equal("User", list[0].Sender.LastName);
    }
}
