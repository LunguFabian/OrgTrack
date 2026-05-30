using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrgTrack.Application.DTOs;
using OrgTrack.Application.UseCases;
using System.Security.Claims;

namespace OrgTrack.Api.Controllers;

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

    private Guid GetUserId()
    {
        return Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<ConversationDto>>> GetConversations()
    {
        var currentUserId = GetUserId();
        var conversations = await _messageService.GetConversationsListAsync(currentUserId);
        return Ok(conversations);
    }

    [HttpGet("conversations/{otherUserId}")]
    public async Task<ActionResult<List<MessageDto>>> GetConversationMessages(Guid otherUserId)
    {
        var currentUserId = GetUserId();
        var messages = await _messageService.GetConversationAsync(currentUserId, otherUserId);
        return Ok(messages);
    }

    [HttpPost("send")]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request)
    {
        var currentUserId = GetUserId();
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Message content cannot be empty." });
        }

        var messageDto = await _messageService.SendMessageAsync(currentUserId, request.ReceiverId, request.Content);
        return Ok(messageDto);
    }

    [HttpPut("conversations/{otherUserId}/read")]
    public async Task<IActionResult> MarkConversationAsRead(Guid otherUserId)
    {
        var currentUserId = GetUserId();
        await _messageService.MarkConversationAsReadAsync(currentUserId, otherUserId);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadTotalCount()
    {
        var currentUserId = GetUserId();
        var count = await _messageService.GetUnreadTotalCountAsync(currentUserId);
        return Ok(new { unreadCount = count });
    }
}

public record SendMessageRequest(Guid ReceiverId, string Content);
