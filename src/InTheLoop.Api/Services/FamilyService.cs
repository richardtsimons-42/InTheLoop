using InTheLoop.Api.Data;
using InTheLoop.Api.DTOs;
using InTheLoop.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace InTheLoop.Api.Services;

public class FamilyService
{
    private readonly ApplicationDbContext _context;

    public FamilyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FamilyDto> CreateFamilyAsync(string ownerId, string name, string? description)
    {
        var family = new Family
        {
            Name = name,
            Description = description,
            OwnerId = ownerId
        };

        _context.Families.Add(family);
        await _context.SaveChangesAsync();

        var member = new FamilyMember
        {
            UserId = ownerId,
            FamilyId = family.Id,
            Role = "admin"
        };
        _context.FamilyMembers.Add(member);
        await _context.SaveChangesAsync();

        return MapToDto(family);
    }

    public async Task<IEnumerable<FamilyDto>> GetUserFamiliesAsync(string userId)
    {
        var familyIds = await _context.FamilyMembers
            .Where(fm => fm.UserId == userId)
            .Select(fm => fm.FamilyId)
            .ToListAsync();

        var families = await _context.Families
            .Include(f => f.Members)
            .Where(f => familyIds.Contains(f.Id))
            .ToListAsync();

        return families.Select(MapToDto);
    }

    public async Task<bool> JoinFamilyAsync(string userId, int familyId)
    {
        var exists = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == userId && fm.FamilyId == familyId);

        if (exists) return false;

        var member = new FamilyMember
        {
            UserId = userId,
            FamilyId = familyId,
            Role = "member"
        };
        _context.FamilyMembers.Add(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveFamilyAsync(string userId, int familyId)
    {
        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == userId && fm.FamilyId == familyId);

        if (member == null) return false;

        _context.FamilyMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    private FamilyDto MapToDto(Family family)
    {
        string ownerName = family.Owner != null
            ? $"{family.Owner.FirstName} {family.Owner.LastName}"
            : "";
        return new FamilyDto(
            family.Id,
            family.Name,
            family.Description,
            ownerName,
            family.Members?.Count ?? 0,
            family.CoverPhotoUrl,
            family.CreatedAt);
    }
}
