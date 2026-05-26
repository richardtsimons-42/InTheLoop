using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Tests.Models;

public class DbContextTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "InTheLoopDbTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CanAddAndQueryUser()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var user = new User
        {
            Id = "db-user-1",
            Email = "dbuser@example.com",
            FirstName = "DB",
            LastName = "User",
            IsVerified = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var found = await context.Users.FirstOrDefaultAsync(u => u.Email == "dbuser@example.com");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("DB", found.FirstName);
        Assert.Equal("User", found.LastName);
        Assert.True(found.IsVerified);
    }

    [Fact]
    public async Task CanAddAndQueryFamily()
    {
        // Arrange
        var context = CreateTestContext();
        var owner = new User { Id = "family-owner", Email = "owner@example.com", FirstName = "Family", LastName = "Owner" };
        context.Users.Add(owner);
        await context.SaveChangesAsync();

        // Act
        var family = new Family
        {
            Name = "Test Family",
            Description = "A test family for DB verification",
            OwnerId = "family-owner"
        };
        context.Families.Add(family);
        await context.SaveChangesAsync();

        var found = await context.Families.FirstOrDefaultAsync(f => f.Name == "Test Family");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test Family", found.Name);
        Assert.Equal("A test family for DB verification", found.Description);
        Assert.Equal("family-owner", found.OwnerId);
    }

    [Fact]
    public async Task CanAddAndQueryPost()
    {
        // Arrange
        var context = CreateTestContext();
        var author = new User { Id = "post-author", Email = "author@example.com", FirstName = "Post", LastName = "Author" };
        var family = new Family { Id = 1, Name = "Post Family", OwnerId = "post-author" };
        context.Users.Add(author);
        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Act
        var post = new Post
        {
            AuthorId = "post-author",
            FamilyId = 1,
            Content = "Test post content"
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var found = await context.Posts.FirstOrDefaultAsync(p => p.Content == "Test post content");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Test post content", found.Content);
        Assert.Equal("post-author", found.AuthorId);
        Assert.Equal(1, found.FamilyId);
    }

    [Fact]
    public async Task CanAddAndQueryMessage()
    {
        // Arrange
        var context = CreateTestContext();
        var sender = new User { Id = "msg-sender", Email = "sender@example.com", FirstName = "Msg", LastName = "Sender" };
        var recipient = new User { Id = "msg-recipient", Email = "recipient@example.com", FirstName = "Msg", LastName = "Recipient" };
        context.Users.Add(sender);
        context.Users.Add(recipient);
        await context.SaveChangesAsync();

        // Act
        var message = new Message
        {
            SenderId = "msg-sender",
            RecipientId = "msg-recipient",
            Content = "Hello from the DB test!"
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        var found = await context.Messages.FirstOrDefaultAsync(m => m.Content == "Hello from the DB test!");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("msg-sender", found.SenderId);
        Assert.Equal("msg-recipient", found.RecipientId);
        Assert.Equal("Hello from the DB test!", found.Content);
    }

    [Fact]
    public async Task CanAddAndQueryPhoto()
    {
        // Arrange
        var context = CreateTestContext();
        var post = new Post { Id = 1, AuthorId = "photo-author", FamilyId = 1, Content = "With photo" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var photo = new Photo
        {
            PostId = 1,
            Url = "/uploads/test-photo.jpg",
            AltText = "A test photo"
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var found = await context.Photos.FirstOrDefaultAsync(p => p.Url == "/uploads/test-photo.jpg");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("/uploads/test-photo.jpg", found.Url);
        Assert.Equal("A test photo", found.AltText);
        Assert.Equal(1, found.PostId);
    }

    [Fact]
    public async Task FamilyMemberHasCompositeKey()
    {
        // Arrange
        var context = CreateTestContext();
        var user = new User { Id = "composite-user", Email = "comp@example.com", FirstName = "Composite", LastName = "User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var member1 = new FamilyMember
        {
            UserId = "composite-user",
            FamilyId = 1,
            Role = "member"
        };
        var member2 = new FamilyMember
        {
            UserId = "composite-user",
            FamilyId = 2,
            Role = "member"
        };
        context.FamilyMembers.AddRange(member1, member2);
        await context.SaveChangesAsync();

        var found = await context.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == "composite-user" && m.FamilyId == 1);

        // Assert
        Assert.NotNull(found);
        Assert.Equal("member", found.Role);

        // Verify the other member also exists
        var found2 = await context.FamilyMembers
            .FirstOrDefaultAsync(m => m.UserId == "composite-user" && m.FamilyId == 2);
        Assert.NotNull(found2);
    }

    [Fact]
    public async Task UserHasAllRequiredProperties()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var user = new User
        {
            Id = "props-user",
            Email = "props@example.com",
            FirstName = "Property",
            LastName = "User",
            AvatarUrl = "https://example.com/avatar.jpg",
            IsVerified = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var found = await context.Users.FirstAsync(u => u.Id == "props-user");

        // Assert
        Assert.Equal("props@example.com", found.Email);
        Assert.Equal("Property", found.FirstName);
        Assert.Equal("User", found.LastName);
        Assert.Equal("https://example.com/avatar.jpg", found.AvatarUrl);
        Assert.True(found.IsVerified);
    }

    [Fact]
    public async Task PostHasAllRequiredProperties()
    {
        // Arrange
        var context = CreateTestContext();
        var author = new User { Id = "post-props-user", Email = "pp@example.com", FirstName = "Post", LastName = "Props" };
        var family = new Family { Id = 1, Name = "Props Family", OwnerId = "post-props-user" };
        context.Users.Add(author);
        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Act
        var post = new Post
        {
            AuthorId = "post-props-user",
            FamilyId = 1,
            Content = "Props test"
        };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        var found = await context.Posts.FirstAsync(p => p.Content == "Props test");

        // Assert
        Assert.Equal("post-props-user", found.AuthorId);
        Assert.Equal(1, found.FamilyId);
        Assert.True(found.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(found.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task MessageHasIsReadProperty()
    {
        // Arrange
        var context = CreateTestContext();
        var sender = new User { Id = "isread-sender", Email = "isread@example.com", FirstName = "Is", LastName = "Read" };
        context.Users.Add(sender);
        await context.SaveChangesAsync();

        // Act
        var message = new Message
        {
            SenderId = "isread-sender",
            Content = "Unread message"
        };
        context.Messages.Add(message);
        await context.SaveChangesAsync();

        var found = await context.Messages.FirstAsync(m => m.Content == "Unread message");

        // Assert
        Assert.False(found.IsRead);
    }

    [Fact]
    public async Task PhotoHasUploadedAtTimestamp()
    {
        // Arrange
        var context = CreateTestContext();
        var post = new Post { Id = 1, AuthorId = "photo-time-user", FamilyId = 1, Content = "Time test" };
        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Act
        var photo = new Photo
        {
            PostId = 1,
            Url = "/uploads/time-test.jpg"
        };
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        var found = await context.Photos.FirstAsync(p => p.Url == "/uploads/time-test.jpg");

        // Assert
        Assert.True(found.UploadedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(found.UploadedAt >= DateTime.UtcNow.AddSeconds(-1));
    }

    [Fact]
    public async Task FamilyMemberHasJoinedAtTimestamp()
    {
        // Arrange
        var context = CreateTestContext();
        var user = new User { Id = "joined-user", Email = "joined@example.com", FirstName = "Joined", LastName = "User" };
        var family = new Family { Id = 1, Name = "Joined Family", OwnerId = "joined-user" };
        context.Users.Add(user);
        context.Families.Add(family);
        await context.SaveChangesAsync();

        // Act
        var member = new FamilyMember
        {
            UserId = "joined-user",
            FamilyId = 1,
            Role = "member"
        };
        context.FamilyMembers.Add(member);
        await context.SaveChangesAsync();

        var found = await context.FamilyMembers.FirstAsync(m => m.UserId == "joined-user");

        // Assert
        Assert.True(found.JoinedAt <= DateTime.UtcNow.AddSeconds(1));
        Assert.True(found.JoinedAt >= DateTime.UtcNow.AddSeconds(-1));
    }
}
