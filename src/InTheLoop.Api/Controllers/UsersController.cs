using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InTheLoop.Api.Services;
using InTheLoop.Api.DTOs;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var profile = await _userService.GetProfileAsync(userId);

        if (profile == null)
            return NotFound(new { message = "User not found" });

        return Ok(profile);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
            return BadRequest(new { message = "First name and last name are required" });

        var profile = await _userService.UpdateProfileAsync(userId, request.FirstName, request.LastName);

        if (profile == null)
            return NotFound(new { message = "User not found" });

        return Ok(profile);
    }

    [HttpPut("me/avatar")]
    public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;

        if (string.IsNullOrWhiteSpace(request.AvatarUrl))
            return BadRequest(new { message = "Avatar URL is required" });

        var profile = await _userService.UpdateAvatarAsync(userId, request.AvatarUrl);

        if (profile == null)
            return NotFound(new { message = "User not found" });

        return Ok(profile);
    }
}

public record UpdateProfileRequest(string FirstName, string LastName);
public record UpdateAvatarRequest(string AvatarUrl);
