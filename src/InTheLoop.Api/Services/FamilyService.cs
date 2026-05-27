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

        return MapToDto(family, ownerId);
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

        return families.Select(f => MapToDto(f, userId));
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

        // Prevent the last admin from leaving
        var adminCount = await _context.FamilyMembers
            .CountAsync(fm => fm.FamilyId == familyId && fm.Role == "admin");

        if (adminCount <= 1 && member.Role == "admin")
            return false;

        _context.FamilyMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FamilyDto?> UpdateFamilyAsync(string userId, int familyId, string? name, string? description, string? coverPhotoUrl)
    {
        var family = await _context.Families
            .Include(f => f.Owner)
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            return null;

        // Allow owners AND admins (co-owners) to update
        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == userId && fm.FamilyId == familyId);

        if (member == null || (member.Role != "admin" && family.OwnerId != userId))
            return null;

        if (!string.IsNullOrEmpty(name))
            family.Name = name;
        if (description != null)
            family.Description = description;
        if (!string.IsNullOrEmpty(coverPhotoUrl))
            family.CoverPhotoUrl = coverPhotoUrl;

        await _context.SaveChangesAsync();
        return MapToDto(family, userId);
    }

    public async Task<bool> InviteMemberAsync(string userId, int familyId, string inviteeEmail)
    {
        var family = await _context.Families
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            return false;

        // Allow owners AND admins (co-owners) to invite
        var member = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == userId && fm.FamilyId == familyId);

        if (member == null || (member.Role != "admin" && family.OwnerId != userId))
            return false;

        // Check if invitee is already a member
        var invitee = await _context.Users.FirstOrDefaultAsync(u => u.Email == inviteeEmail);
        if (invitee == null)
            return false;

        var alreadyMember = await _context.FamilyMembers
            .AnyAsync(fm => fm.UserId == invitee.Id && fm.FamilyId == familyId);

        if (alreadyMember)
            return false;

        var newMember = new FamilyMember
        {
            UserId = invitee.Id,
            FamilyId = familyId,
            Role = "member"
        };
        _context.FamilyMembers.Add(newMember);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PromoteMemberAsync(string requestingUserId, int familyId, string targetUserId)
    {
        var family = await _context.Families
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            return false;

        // Only the owner can promote members (co-owners can't promote others)
        if (family.OwnerId != requestingUserId)
            return false;

        // Can't promote the owner
        if (targetUserId == family.OwnerId)
            return false;

        var targetMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == targetUserId && fm.FamilyId == familyId);

        if (targetMember == null)
            return false;

        targetMember.Role = "admin";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DemoteMemberAsync(string requestingUserId, int familyId, string targetUserId)
    {
        var family = await _context.Families
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            return false;

        // Only the owner can demote members
        if (family.OwnerId != requestingUserId)
            return false;

        // Can't demote the owner
        if (targetUserId == family.OwnerId)
            return false;

        var targetMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == targetUserId && fm.FamilyId == familyId);

        if (targetMember == null)
            return false;

        // Ensure at least one admin remains
        var adminCount = await _context.FamilyMembers
            .CountAsync(fm => fm.FamilyId == familyId && fm.Role == "admin");

        if (adminCount <= 1)
            return false;

        targetMember.Role = "member";
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveMemberAsync(string requestingUserId, int familyId, string targetUserId)
    {
        var family = await _context.Families
            .Include(f => f.Members)
            .FirstOrDefaultAsync(f => f.Id == familyId);

        if (family == null)
            return false;

        // Only the owner can remove members (co-owners can't remove others)
        if (family.OwnerId != requestingUserId)
            return false;

        // Can't remove the owner
        if (targetUserId == family.OwnerId)
            return false;

        var targetMember = await _context.FamilyMembers
            .FirstOrDefaultAsync(fm => fm.UserId == targetUserId && fm.FamilyId == familyId);

        if (targetMember == null)
            return false;

        _context.FamilyMembers.Remove(targetMember);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<FamilyMemberDto>> GetFamilyMembersAsync(int familyId)
    {
        var members = await _context.FamilyMembers
            .Include(fm => fm.User)
            .Where(fm => fm.FamilyId == familyId)
            .ToListAsync();

        return members.Select(MapToMemberDto);
    }

    private FamilyDto MapToDto(Family family, string currentUserId)
    {
        string ownerName = family.Owner != null
            ? $"{family.Owner.FirstName} {family.Owner.LastName}"
            : "";

        // Find current user's role
        var currentMember = family.Members?.FirstOrDefault(m => m.UserId == currentUserId);
        string userRole = currentMember?.Role ?? "none";

        return new FamilyDto(
            family.Id,
            family.Name,
            family.Description,
            ownerName,
            family.Members?.Count ?? 0,
            family.CoverPhotoUrl,
            family.CreatedAt,
            userRole);
    }

    private FamilyMemberDto MapToMemberDto(FamilyMember member)
    {
        return new FamilyMemberDto(
            member.Id,
            member.UserId,
            member.User != null ? $"{member.User.FirstName} {member.User.LastName}" : "",
            member.User?.AvatarUrl,
            member.Role,
            member.JoinedAt);
    }
}
