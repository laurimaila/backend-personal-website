using backend.DTOs;
using backend.Middleware;
using backend.Models;
using backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController(
    IMessageService messageService,
    IWebSocketService webSocketService,
    IValidationService validationService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponseDto>>> GetMessages([FromQuery] int limit = 50)
    {
        var messages = await messageService.GetRecentMessagesAsync(limit);
        return Ok(messages);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<MessageResponseDto>>> GetMessagesPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await messageService.GetMessagesPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteMessage(int id)
    {
        await messageService.DeleteMessageAsync(id, CurrentUserId, CurrentIsAdmin);
        return NoContent();
    }

    [HttpPost("{id:int}/reactions")]
    public async Task<ActionResult> ReactToMessage(int id, [FromBody] ReactToMessageDto dto)
    {
        validationService.ValidateAndThrow(dto);
        var reactionEvent = await messageService.ReactToMessageAsync(id, CurrentUserId, dto.Emoji);
        await webSocketService.BroadcastMessage(WebSocketMessageTypes.Reaction, reactionEvent);
        return NoContent();
    }

    [HttpDelete("{id:int}/reactions")]
    public async Task<ActionResult> RemoveReaction(int id)
    {
        var reactionEvent = await messageService.RemoveReactionAsync(id, CurrentUserId);
        if (reactionEvent == null)
        {
            throw new ApiException("REACTION_NOT_FOUND", "No reaction found to remove", System.Net.HttpStatusCode.NotFound);
        }

        await webSocketService.BroadcastMessage(WebSocketMessageTypes.Reaction, reactionEvent);
        return NoContent();
    }

}
