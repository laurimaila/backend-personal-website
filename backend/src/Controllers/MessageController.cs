using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Services;

namespace backend.Controllers;

[ApiController]
[Route("api/messages")]
public class MessagesController(IMessageService messageService, ILogger<MessagesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessages([FromQuery] int limit = 50)
    {
        try
        {
            var messages = await messageService.GetRecentMessagesAsync(limit);
            return Ok(messages);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching messages");
            return StatusCode(500, "An error occurred while fetching messages");
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResult<Message>>> GetMessagesPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var result = await messageService.GetMessagesPagedAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching paged messages");
            return StatusCode(500, "An error occurred while fetching messages");
        }
    }

    [HttpGet("tukaani")]
    public async Task<ActionResult<bool>> TestDeleteAllMessages()
    {
        try
        {
            var result = await messageService.DeleteAllMessagesAsync();
            if (result)
            {
                return Ok(new { message = "All messages successfully deleted" });
            }

            return NotFound(new { message = "No messages to delete" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting all messages");
            return StatusCode(500, new { message = "An error occurred while deleting messages" });
        }
    }
}
