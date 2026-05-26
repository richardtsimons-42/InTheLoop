using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InTheLoop.Api.Tests.Controllers;

public class ContactsControllerTests
{
    private ApplicationDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new ApplicationDbContext(options);
    }

    private ClaimsPrincipal CreateFakeUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "FakeAuth");
        return new ClaimsPrincipal(identity);
    }

    private ContactsController CreateController(ApplicationDbContext context)
    {
        var controller = new ContactsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    private List<Dictionary<string, object>> GetConversationList(ObjectResult result)
    {
        var json = JsonSerializer.Serialize(result.Value);
        var list = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
        return list?.Select(d => d.ToDictionary(k => k.Key, v => (object)v.Value)).ToList() ?? new List<Dictionary<string, object>>();
    }

    [Fact]
    public async Task GetConversations_ReturnsEmpty_WhenNoMessages()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Empty(list);

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_ReturnsDmConversation_WhenMessagesExist()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var user = new User
        {
            Id = userId, UserName = "test@test.com", Email = "test@test.com",
            FirstName = "Test", LastName = "User"
        };
        var otherUser = new User
        {
            Id = otherUserId, UserName = "other@test.com", Email = "other@test.com",
            FirstName = "Other", LastName = "User"
        };
        context.Users.AddRange(user, otherUser);
        await context.SaveChangesAsync();

        context.Messages.Add(new Message
        {
            Id = 1, SenderId = otherUserId, RecipientId = userId,
            Content = "Hello!", SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Single(list);
        Assert.Equal("dm", list[0]["conversationType"].ToString());
        Assert.Equal("Other User", list[0]["partnerName"].ToString());
        Assert.Equal("Hello!", list[0]["lastMessage"].ToString());

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_ReturnsFamilyConversation_WhenMessagesExist()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();

        var family = new Family { Id = 1, Name = "Smith Family", OwnerId = userId };
        context.Families.Add(family);

        context.FamilyMembers.Add(new FamilyMember
        {
            UserId = userId, FamilyId = 1, Role = "member"
        });

        var otherUser = new User
        {
            Id = Guid.NewGuid().ToString(), UserName = "other@test.com", Email = "other@test.com",
            FirstName = "Jane", LastName = "Smith"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        context.Messages.Add(new Message
        {
            Id = 1, SenderId = otherUser.Id, FamilyId = 1,
            Content = "Dinner at 7!", SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Single(list);
        Assert.Equal("family", list[0]["conversationType"].ToString());
        Assert.Equal("Smith Family", list[0]["partnerName"].ToString());

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_ExcludesNonMemberFamilyMessages()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var family = new Family { Id = 1, Name = "Other Family", OwnerId = otherUserId };
        context.Families.Add(family);

        context.Messages.Add(new Message
        {
            Id = 1, SenderId = otherUserId, FamilyId = 1,
            Content = "Secret message", SentAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Empty(list);

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_GroupsMultipleMessagesToSameConversation()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var otherUser = new User
        {
            Id = otherUserId, UserName = "other@test.com", Email = "other@test.com",
            FirstName = "John", LastName = "Doe"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        for (int i = 0; i < 5; i++)
        {
            context.Messages.Add(new Message
            {
                Id = i + 1,
                SenderId = i % 2 == 0 ? userId : otherUserId,
                RecipientId = i % 2 == 0 ? otherUserId : userId,
                Content = $"Message {i + 1}",
                SentAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Single(list);
        Assert.Equal("dm", list[0]["conversationType"].ToString());
        Assert.Equal("John Doe", list[0]["partnerName"].ToString());

        context.Dispose();
    }

    [Fact]
    public async Task GetConversationMessages_ReturnsMessages_ForDm()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var otherUser = new User
        {
            Id = otherUserId, UserName = "other@test.com", Email = "other@test.com",
            FirstName = "Jane", LastName = "Doe"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        context.Messages.Add(new Message { Id = 1, SenderId = otherUserId, RecipientId = userId, Content = "Hi", SentAt = DateTime.UtcNow.AddMinutes(-10) });
        context.Messages.Add(new Message { Id = 2, SenderId = userId, RecipientId = otherUserId, Content = "Hello!", SentAt = DateTime.UtcNow.AddMinutes(-5) });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversationMessages($"dm-{otherUserId}");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.True(list.Count >= 1, $"Expected at least 1 message but got {list.Count}");
        Assert.Contains("Hi", list.Select(m => m["Content"].ToString()));

        context.Dispose();
    }

    [Fact]
    public async Task GetConversationMessages_MarksDmAsRead()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var otherUser = new User
        {
            Id = otherUserId, UserName = "other@test.com", Email = "other@test.com",
            FirstName = "Jane", LastName = "Doe"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        context.Messages.Add(new Message
        {
            Id = 1, SenderId = otherUserId, RecipientId = userId,
            Content = "Unread message", SentAt = DateTime.UtcNow, IsRead = false
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        await controller.GetConversationMessages($"dm-{otherUserId}");

        var updatedMessage = await context.Messages.FindAsync(1);
        Assert.NotNull(updatedMessage);
        Assert.True(updatedMessage.IsRead);

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_ReturnsUnreadCount_ForDm()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();
        var otherUserId = Guid.NewGuid().ToString();

        var otherUser = new User
        {
            Id = otherUserId, UserName = "other@test.com", Email = "other@test.com",
            FirstName = "Jane", LastName = "Doe"
        };
        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        for (int i = 0; i < 3; i++)
        {
            context.Messages.Add(new Message
            {
                Id = i + 1, SenderId = otherUserId, RecipientId = userId,
                Content = $"Unread {i + 1}", SentAt = DateTime.UtcNow.AddMinutes(i), IsRead = false
            });
        }
        await context.SaveChangesAsync();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversations();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = GetConversationList(okResult);
        Assert.Single(list);
        Assert.Equal(3, ((JsonElement)list[0]["unreadCount"]).GetInt32());

        context.Dispose();
    }

    [Fact]
    public async Task GetConversationMessages_ReturnsBadRequest_ForInvalidId()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);
        var userId = Guid.NewGuid().ToString();

        var controller = CreateController(context);
        controller.ControllerContext.HttpContext.User = CreateFakeUser(userId);

        var result = await controller.GetConversationMessages("invalid-id");

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid conversation ID", badResult.Value);

        context.Dispose();
    }

    [Fact]
    public async Task GetConversations_ReturnsUnauthorized_WhenNoUserId()
    {
        var dbName = $"ContactsTest_{Guid.NewGuid()}";
        var context = CreateDbContext(dbName);

        var controller = new ContactsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var result = await controller.GetConversations();

        Assert.IsType<UnauthorizedResult>(result);

        context.Dispose();
    }
}
