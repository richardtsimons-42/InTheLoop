using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InTheLoop.Api.Services;

namespace InTheLoop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly MessageService _messageService;

    public MessagesController(MessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] SendMessageRequest request)
    {
        var senderId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        await _messageService.SendMessageAsync(senderId, request.RecipientId, request.FamilyId, request.Content);
        return Ok();
    }

    [HttpGet("conversation")]
    public async Task<IActionResult> GetConversation(
        [FromQuery] string? recipientId,
        [FromQuery] int? familyId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        var messages = await _messageService.GetConversationAsync(userId, recipientId, familyId);
        return Ok(messages);
    }
}

public record SendMessageRequest(string? RecipientId, int? FamilyId, string Content);
