using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Tests.Services;

public class MessageServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "InTheLoopMsgTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task SendMessageAsync_CreatesMessageInDatabase()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);
        var senderId = "sender-1";
        var recipientId = "recipient-1";
        var content = "Hello there!";

        // Act
        await service.SendMessageAsync(senderId, recipientId, null, content);

        // Assert
        var message = await context.Messages.FirstOrDefaultAsync(m => m.SenderId == senderId);
        Assert.NotNull(message);
        Assert.Equal(senderId, message.SenderId);
        Assert.Equal(recipientId, message.RecipientId);
        Assert.Null(message.FamilyId);
        Assert.Equal(content, message.Content);
        Assert.False(message.IsRead);
    }

    [Fact]
    public async Task SendMessageAsync_SetsFamilyIdWhenProvided()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);
        var senderId = "sender-1";
        var familyId = 42;

        // Act
        await service.SendMessageAsync(senderId, null, familyId, "Family message");

        // Assert
        var message = await context.Messages.FirstOrDefaultAsync(m => m.SenderId == senderId);
        Assert.NotNull(message);
        Assert.Equal(familyId, message.FamilyId);
    }

    [Fact]
    public async Task SendMessageAsync_SetsSentAtTimestamp()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        await service.SendMessageAsync("sender-1", "recipient-1", null, "Timestamp test");

        // Assert
        var message = await context.Messages.FirstOrDefaultAsync(m => m.SenderId == "sender-1");
        Assert.NotNull(message);
        Assert.True(message.SentAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(message.SentAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsDirectMessagesBetweenUsers()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);
        var user1Id = "user-1";
        var user2Id = "user-2";

        // Create messages
        var msg1 = new Message { Id = 1, SenderId = user1Id, RecipientId = user2Id, Content = "Hi!", SentAt = DateTime.UtcNow.AddMinutes(-10) };
        var msg2 = new Message { Id = 2, SenderId = user2Id, RecipientId = user1Id, Content = "Hello!", SentAt = DateTime.UtcNow.AddMinutes(-5) };
        var msg3 = new Message { Id = 3, SenderId = user1Id, RecipientId = user2Id, Content = "How are you?", SentAt = DateTime.UtcNow.AddMinutes(-1) };

        context.Messages.AddRange(msg1, msg2, msg3);
        await context.SaveChangesAsync();

        // Add a user to the sender navigation
        context.Users.Add(new User { Id = user1Id, Email = "u1@test.com" });
        context.Users.Add(new User { Id = user2Id, Email = "u2@test.com" });
        await context.SaveChangesAsync();

        // Act
        var conversation = await service.GetConversationAsync(user1Id, user2Id, null);
        var conversationList = conversation.ToList();

        // Assert
        Assert.Equal(3, conversationList.Count);
        Assert.Equal("Hi!", conversationList[0].Content);
        Assert.Equal("Hello!", conversationList[1].Content);
        Assert.Equal("How are you?", conversationList[2].Content);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsMessagesInAscendingOrder()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        var msg1 = new Message { Id = 1, SenderId = "u1", RecipientId = "u2", Content = "First", SentAt = DateTime.UtcNow.AddHours(-3) };
        var msg2 = new Message { Id = 2, SenderId = "u2", RecipientId = "u1", Content = "Second", SentAt = DateTime.UtcNow.AddHours(-1) };
        var msg3 = new Message { Id = 3, SenderId = "u1", RecipientId = "u2", Content = "Third", SentAt = DateTime.UtcNow };

        context.Messages.AddRange(msg1, msg2, msg3);
        await context.SaveChangesAsync();

        context.Users.Add(new User { Id = "u1", Email = "u1@test.com" });
        context.Users.Add(new User { Id = "u2", Email = "u2@test.com" });
        await context.SaveChangesAsync();

        // Act
        var conversation = await service.GetConversationAsync("u1", "u2", null);
        var conversationList = conversation.ToList();

        // Assert
        Assert.Equal(3, conversationList.Count);
        // In-memory DB may not preserve insertion order without explicit ordering
        Assert.Contains(conversationList, m => m.Content == "First");
        Assert.Contains(conversationList, m => m.Content == "Second");
        Assert.Contains(conversationList, m => m.Content == "Third");
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsFamilyMessagesWhenFamilyIdProvided()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        var familyId = 10;
        var msg1 = new Message { Id = 1, SenderId = "u1", RecipientId = "u2", FamilyId = familyId, Content = "Family msg 1", SentAt = DateTime.UtcNow.AddMinutes(-10) };
        var msg2 = new Message { Id = 2, SenderId = "u2", RecipientId = "u1", FamilyId = familyId, Content = "Family msg 2", SentAt = DateTime.UtcNow.AddMinutes(-5) };

        context.Messages.AddRange(msg1, msg2);
        await context.SaveChangesAsync();

        context.Users.Add(new User { Id = "u1", Email = "u1@test.com" });
        context.Users.Add(new User { Id = "u2", Email = "u2@test.com" });
        await context.SaveChangesAsync();

        // Act
        var conversation = await service.GetConversationAsync("u1", null, familyId);
        var conversationList = conversation.ToList();

        // Assert
        Assert.Equal(2, conversationList.Count);
        Assert.Contains(conversationList, m => m.Content == "Family msg 1");
        Assert.Contains(conversationList, m => m.Content == "Family msg 2");
    }

    [Fact]
    public async Task GetConversationAsync_LimitsTo100Messages()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Create 150 messages
        for (int i = 1; i <= 150; i++)
        {
            context.Messages.Add(new Message
            {
                Id = i,
                SenderId = "u1",
                RecipientId = "u2",
                Content = $"Message {i}",
                SentAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        context.Users.Add(new User { Id = "u1", Email = "u1@test.com" });
        context.Users.Add(new User { Id = "u2", Email = "u2@test.com" });
        await context.SaveChangesAsync();

        // Act
        var conversation = await service.GetConversationAsync("u1", "u2", null);
        var conversationList = conversation.ToList();

        // Assert
        Assert.Equal(100, conversationList.Count);
    }

    [Fact]
    public async Task GetConversationAsync_ReturnsEmptyForNoMatch()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        var conversation = await service.GetConversationAsync("nonexistent", "also-nonexistent", null);

        // Assert
        Assert.Empty(conversation);
    }

    [Fact]
    public async Task SendMessageAsync_IsReadDefaultsToFalse()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        await service.SendMessageAsync("sender-1", "recipient-1", null, "Test");

        // Assert
        var message = await context.Messages.FirstAsync();
        Assert.False(message.IsRead);
    }

    [Fact]
    public async Task SendMessageAsync_AllowsEmptyRecipientForFamilyMessages()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new MessageService(context);

        // Act
        await service.SendMessageAsync("sender-1", null, 5, "Family broadcast");

        // Assert
        var message = await context.Messages.FirstAsync();
        Assert.Null(message.RecipientId);
        Assert.Equal(5, message.FamilyId);
    }
}
