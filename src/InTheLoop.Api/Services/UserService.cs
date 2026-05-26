using InTheLoop.Api.Data;
using InTheLoop.Api.DTOs;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class UserService
{
    private readonly ApplicationDbContext _context;

    public UserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponseDto> GetProfileAsync(string userId)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null!;

        return new ProfileResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.IsVerified,
            user.CreatedAt);
    }

    public async Task<ProfileResponseDto> UpdateProfileAsync(string userId, string firstName, string lastName)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null!;

        user.FirstName = firstName;
        user.LastName = lastName;
        await _context.SaveChangesAsync();

        return new ProfileResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.IsVerified,
            user.CreatedAt);
    }

    public async Task<ProfileResponseDto> UpdateAvatarAsync(string userId, string avatarUrl)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null!;

        user.AvatarUrl = avatarUrl;
        await _context.SaveChangesAsync();

        return new ProfileResponseDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
            user.IsVerified,
            user.CreatedAt);
    }
}
