using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class UserServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "UserTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsProfileForExistingUser()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        var user = new User
        {
            Id = "profile-user",
            Email = "profile@example.com",
            FirstName = "Profile",
            LastName = "User",
            AvatarUrl = "https://example.com/avatar.png",
            IsVerified = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProfileAsync("profile-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("profile-user", result.Id);
        Assert.Equal("profile@example.com", result.Email);
        Assert.Equal("Profile", result.FirstName);
        Assert.Equal("User", result.LastName);
        Assert.Equal("https://example.com/avatar.png", result.AvatarUrl);
        Assert.True(result.IsVerified);
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNullForNonExistingUser()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        // Act
        var result = await service.GetProfileAsync("non-existent-user");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesName()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        var user = new User
        {
            Id = "update-user",
            Email = "update@example.com",
            FirstName = "Old",
            LastName = "Name"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateProfileAsync("update-user", "New", "Name");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New", result.FirstName);
        Assert.Equal("Name", result.LastName);
        Assert.Equal("update@example.com", result.Email);

        // Verify in DB
        var dbUser = await context.Users.FirstAsync(u => u.Id == "update-user");
        Assert.Equal("New", dbUser.FirstName);
        Assert.Equal("Name", dbUser.LastName);
    }

    [Fact]
    public async Task UpdateProfileAsync_ReturnsNullForNonExistingUser()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        // Act
        var result = await service.UpdateProfileAsync("non-existent", "New", "Name");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAvatarAsync_UpdatesAvatar()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        var user = new User
        {
            Id = "avatar-user",
            Email = "avatar@example.com",
            FirstName = "Avatar",
            LastName = "User"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateAvatarAsync("avatar-user", "https://example.com/new-avatar.png");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://example.com/new-avatar.png", result.AvatarUrl);

        // Verify in DB
        var dbUser = await context.Users.FirstAsync(u => u.Id == "avatar-user");
        Assert.Equal("https://example.com/new-avatar.png", dbUser.AvatarUrl);
    }

    [Fact]
    public async Task UpdateAvatarAsync_ReturnsNullForNonExistingUser()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        // Act
        var result = await service.UpdateAvatarAsync("non-existent", "https://example.com/avatar.png");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileAsync_PreservesCreatedAt()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        var user = new User
        {
            Id = "created-at-user",
            Email = "created@example.com",
            FirstName = "Created",
            LastName = "At"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetProfileAsync("created-at-user");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CreatedAt > DateTime.UnixEpoch);
        Assert.True(result.CreatedAt <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task UpdateProfileAsync_PreservesEmailAndAvatar()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new UserService(context);

        var user = new User
        {
            Id = "preserve-user",
            Email = "preserve@example.com",
            FirstName = "Old",
            LastName = "Name",
            AvatarUrl = "https://example.com/old.png"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdateProfileAsync("preserve-user", "New", "Name");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("preserve@example.com", result.Email);
        Assert.Equal("https://example.com/old.png", result.AvatarUrl);
    }
}
