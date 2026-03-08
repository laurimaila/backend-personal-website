using backend.Data.Entities;
using backend.Middleware;
using backend.Models;
using backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public class MessagesController(IMessageService messageService) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessages([FromQuery] int limit = 50)
    {
        var messages = await messageService.GetRecentMessagesAsync(limit);
        return Ok(messages);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<Message>>> GetMessagesPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await messageService.GetMessagesPagedAsync(page, pageSize);
        return Ok(result);
    }

    [HttpGet("tukaani")]
    [AllowAnonymous]
    public async Task<ActionResult<bool>> TestDeleteAllMessages()
    {
        var result = await messageService.DeleteAllMessagesAsync();
        if (result)
        {
            return Ok(new { message = "All messages successfully deleted" });
        }

        throw new ApiException("MESSAGES_NOT_FOUND", "No messages to delete", System.Net.HttpStatusCode.NotFound);
    }
}
