using backend.DTOs;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public interface IMessageService
{
    Task<IEnumerable<Message>> GetRecentMessagesAsync(int limit);
    Task<PagedResult<Message>> GetMessagesPagedAsync(int page, int pageSize);

    Task<Message> CreateMessageAsync(CreateMessageDto createMessageDto, User authenticatedUser);
    Task<bool> DeleteAllMessagesAsync();
}

public class MessageService(IMessageRepository repository, ILogger<MessageService> logger) : IMessageService
{
    public async Task<IEnumerable<Message>> GetRecentMessagesAsync(int limit)
    {
        try
        {
            return await repository.GetMessagesAsync(limit);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recent messages");
            throw;
        }
    }

    public async Task<PagedResult<Message>> GetMessagesPagedAsync(int page, int pageSize)
    {
        try
        {
            var (messages, total) = await repository.GetMessagesPagedAsync(page, pageSize);

            return new PagedResult<Message>
            {
                Items = messages,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting paged messages");
            throw;
        }
    }

    public async Task<Message> CreateMessageAsync(CreateMessageDto createMessageDto, User authenticatedUser)
    {
        try
        {
            var message = new Message
            {
                Content = createMessageDto.content,
                Creator = authenticatedUser.Username,
                CreatedAt = DateTime.UtcNow
            };

            logger.LogInformation("Creating message for user {Username}", authenticatedUser.Username);
            return await repository.CreateMessageAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating message");
            throw;
        }
    }

    public async Task<bool> DeleteAllMessagesAsync()
    {
        try
        {
            return await repository.DeleteAllMessagesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting all messages");
            throw;
        }
    }
}
