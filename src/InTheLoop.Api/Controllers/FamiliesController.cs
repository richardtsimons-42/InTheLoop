using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InTheLoop.Api.Services;
using InTheLoop.Api.DTOs;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FamiliesController : ControllerBase
{
    private readonly FamilyService _familyService;

    public FamiliesController(FamilyService familyService)
    {
        _familyService = familyService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateFamily([FromBody] CreateFamilyRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var family = await _familyService.CreateFamilyAsync(userId, request.Name, request.Description);
        return Ok(family);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserFamilies()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var families = await _familyService.GetUserFamiliesAsync(userId);
        return Ok(families);
    }

    [HttpGet("{familyId}/members")]
    public async Task<IActionResult> GetFamilyMembers(int familyId)
    {
        var members = await _familyService.GetFamilyMembersAsync(familyId);
        return Ok(members);
    }

    [HttpPost("{familyId}/join")]
    public async Task<IActionResult> JoinFamily(int familyId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.JoinFamilyAsync(userId, familyId);
        return Ok(result);
    }

    [HttpPost("{familyId}/leave")]
    public async Task<IActionResult> LeaveFamily(int familyId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.LeaveFamilyAsync(userId, familyId);
        if (!result)
            return NotFound(new { message = "Not a member of this family" });
        return Ok(new { message = "Left family successfully" });
    }

    [HttpPut("{familyId}")]
    public async Task<IActionResult> UpdateFamily(int familyId, [FromBody] UpdateFamilyRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.UpdateFamilyAsync(userId, familyId, request.Name, request.Description, request.CoverPhotoUrl);
        if (result == null)
            return NotFound(new { message = "Family not found or not authorized" });
        return Ok(result);
    }

    [HttpPost("{familyId}/invite")]
    public async Task<IActionResult> InviteMember(int familyId, [FromBody] InviteMemberRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.InviteMemberAsync(userId, familyId, request.Email);
        if (!result)
            return BadRequest(new { message = "Failed to invite member. User may not exist or already a member." });
        return Ok(new { message = "Member invited successfully" });
    }

    [HttpPost("{familyId}/promote")]
    public async Task<IActionResult> PromoteMember(int familyId, [FromBody] PromoteMemberRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.PromoteMemberAsync(userId, familyId, request.UserId);
        if (!result)
            return BadRequest(new { message = "Failed to promote member. Only the owner can promote, and you can't promote yourself." });
        return Ok(new { message = "Member promoted to co-owner" });
    }

    [HttpPost("{familyId}/demote")]
    public async Task<IActionResult> DemoteMember(int familyId, [FromBody] DemoteMemberRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.DemoteMemberAsync(userId, familyId, request.UserId);
        if (!result)
            return BadRequest(new { message = "Failed to demote member. Only the owner can demote, and at least one admin must remain." });
        return Ok(new { message = "Member demoted to regular member" });
    }

    [HttpPost("{familyId}/remove")]
    public async Task<IActionResult> RemoveMember(int familyId, [FromBody] RemoveMemberRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.RemoveMemberAsync(userId, familyId, request.UserId);
        if (!result)
            return BadRequest(new { message = "Failed to remove member. Only the owner can remove, and you can't remove yourself." });
        return Ok(new { message = "Member removed from family" });
    }
}

public record CreateFamilyRequest(string Name, string? Description);
public record UpdateFamilyRequest(string? Name, string? Description, string? CoverPhotoUrl);
public record InviteMemberRequest(string Email);
public record PromoteMemberRequest(string UserId);
public record DemoteMemberRequest(string UserId);
public record RemoveMemberRequest(string UserId);
