using InTheLoop.Api.Data;
using InTheLoop.Api.Models;
using InTheLoop.Api.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Tests.Services;

public class FamilyServiceTests
{
    private ApplicationDbContext CreateTestContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "FamilyTest_" + Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateFamilyAsync_CreatesFamilyAndMember()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        // Act
        var result = await service.CreateFamilyAsync("user-1", "Test Family", "Test description");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Family", result.Name);
        Assert.Equal("Test description", result.Description);
        Assert.Equal("", result.OwnerName); // Owner not created in DB
        Assert.Equal(1, result.MemberCount);
        Assert.True(result.CreatedAt > DateTime.UnixEpoch);

        // Verify in DB
        var family = await context.Families.FirstAsync(f => f.Name == "Test Family");
        Assert.NotNull(family);
        var member = await context.FamilyMembers.FirstAsync(m => m.UserId == "user-1");
        Assert.Equal("admin", member.Role);
    }

    [Fact]
    public async Task CreateFamilyAsync_CreatesFamilyWithoutDescription()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        // Act
        var result = await service.CreateFamilyAsync("user-2", "No Desc Family", null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("No Desc Family", result.Name);
        Assert.Null(result.Description);
    }

    [Fact]
    public async Task GetUserFamiliesAsync_ReturnsCorrectFamilies()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("user-1", "Family 1", "First");
        await service.CreateFamilyAsync("user-1", "Family 2", "Second");

        // Act
        var result = (await service.GetUserFamiliesAsync("user-1")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.Name == "Family 1");
        Assert.Contains(result, f => f.Name == "Family 2");
    }

    [Fact]
    public async Task GetUserFamiliesAsync_ReturnsEmptyForNoFamilies()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        // Act
        var result = await service.GetUserFamiliesAsync("user-0");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserFamiliesAsync_ReturnsMemberCount()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Count Family", "Count test");

        // Add extra members
        var family = await context.Families.FirstAsync(f => f.Name == "Count Family");
        context.FamilyMembers.AddRange(
            new FamilyMember { UserId = "member-2", FamilyId = family.Id, Role = "member" },
            new FamilyMember { UserId = "member-3", FamilyId = family.Id, Role = "member" }
        );
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUserFamiliesAsync("owner-1");

        // Assert
        Assert.Single(result);
        Assert.Equal(3, result.First().MemberCount);
    }

    [Fact]
    public async Task JoinFamilyAsync_CreatesMemberWithCorrectRole()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Join Family", "Join test");

        // Act
        var result = await service.JoinFamilyAsync("member-1", 1);

        // Assert
        Assert.True(result);
        var member = await context.FamilyMembers.FirstAsync(m => m.UserId == "member-1");
        Assert.Equal("member", member.Role);
    }

    [Fact]
    public async Task JoinFamilyAsync_ReturnsTrueWhenNotAlreadyMember()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Join Test", "Test");

        // Act
        var result = await service.JoinFamilyAsync("new-member", 1);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task JoinFamilyAsync_ReturnsFalseWhenAlreadyMember()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Already Member", "Test");
        await service.JoinFamilyAsync("existing-member", 1);

        // Act
        var result = await service.JoinFamilyAsync("existing-member", 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LeaveFamilyAsync_RemovesMemberFromFamily()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Leave Test", "Test");
        await service.JoinFamilyAsync("member-1", 1);

        // Act
        var result = await service.LeaveFamilyAsync("member-1", 1);

        // Assert
        Assert.True(result);
        Assert.False(await context.FamilyMembers.AnyAsync(m => m.UserId == "member-1"));
    }

    [Fact]
    public async Task LeaveFamilyAsync_ReturnsFalseWhenNotMember()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Not Member", "Test");

        // Act
        var result = await service.LeaveFamilyAsync("non-member", 1);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task LeaveFamilyAsync_RemovesMemberButKeepsFamily()
    {
        // Arrange
        var context = CreateTestContext();
        var service = new FamilyService(context);

        await service.CreateFamilyAsync("owner-1", "Stay Family", "Test");
        await service.JoinFamilyAsync("member-1", 1);

        // Act
        await service.LeaveFamilyAsync("member-1", 1);

        // Assert
        var family = await context.Families.FindAsync(1);
        Assert.NotNull(family);
        Assert.Equal("Stay Family", family.Name);
        var owner = await context.FamilyMembers.FindAsync("owner-1", 1);
        Assert.NotNull(owner);
    }
}
