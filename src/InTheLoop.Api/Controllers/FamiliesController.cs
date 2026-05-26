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

    [HttpPost("{familyId}/join")]
    public async Task<IActionResult> JoinFamily(int familyId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var result = await _familyService.JoinFamilyAsync(userId, familyId);
        return Ok(result);
    }
}

public record CreateFamilyRequest(string Name, string? Description);
