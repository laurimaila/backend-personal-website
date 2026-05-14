using backend.Data.Entities;
using backend.DTOs;
using backend.Middleware;
using backend.Models;
using backend.Repositories;

namespace backend.Services;

public interface IMessageService
{
    Task<IEnumerable<MessageResponseDto>> GetRecentMessagesAsync(int limit);
    Task<PagedResult<MessageResponseDto>> GetMessagesPagedAsync(int page, int pageSize);
    Task<MessageResponseDto> CreateMessageAsync(CreateMessageDto createMessageDto, int creatorId);
    Task DeleteMessageAsync(int messageId, int requestingUserId, bool isAdmin);
    Task<ReactionEventDto> ReactToMessageAsync(int messageId, int userId, string emoji);
    Task<ReactionEventDto?> RemoveReactionAsync(int messageId, int userId);
    Task<bool> DeleteAllMessagesAsync();
}

public class MessageService(
    IMessageRepository repository,
    IMessageReactionRepository reactionRepository,
    IUserRepository userRepository,
    ILogger<MessageService> logger) : IMessageService
{
    public async Task<IEnumerable<MessageResponseDto>> GetRecentMessagesAsync(int limit)
    {
        try
        {
            var messages = await repository.GetMessagesAsync(limit);
            return messages.Select(MessageResponseDto.FromEntity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting recent messages");
            throw;
        }
    }

    public async Task<PagedResult<MessageResponseDto>> GetMessagesPagedAsync(int page, int pageSize)
    {
        try
        {
            var (messages, total) = await repository.GetMessagesPagedAsync(page, pageSize);

            return new PagedResult<MessageResponseDto>
            {
                Items = messages.Select(MessageResponseDto.FromEntity),
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

    public async Task<MessageResponseDto> CreateMessageAsync(CreateMessageDto createMessageDto, int creatorId)
    {
        try
        {
            var message = new Message
            {
                Content = createMessageDto.content,
                CreatorId = creatorId,
                CreatedAt = DateTime.UtcNow
            };

            logger.LogInformation("Creating message for user {UserId}", creatorId);
            var saved = await repository.CreateMessageAsync(message);
            return MessageResponseDto.FromEntity(saved);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating message");
            throw;
        }
    }

    public async Task DeleteMessageAsync(int messageId, int requestingUserId, bool isAdmin)
    {
        var message = await repository.GetMessageByIdAsync(messageId);
        if (message == null)
        {
            throw new ApiException("MESSAGE_NOT_FOUND", "Message not found", System.Net.HttpStatusCode.NotFound);
        }

        if (!isAdmin && message.CreatorId != requestingUserId)
        {
            throw new ApiException("FORBIDDEN", "You can only delete your own messages", System.Net.HttpStatusCode.Forbidden);
        }

        await repository.DeleteMessageAsync(message);
    }

    public async Task<ReactionEventDto> ReactToMessageAsync(int messageId, int userId, string emoji)
    {
        var message = await repository.GetMessageByIdAsync(messageId);
        if (message == null)
        {
            throw new ApiException("MESSAGE_NOT_FOUND", "Message not found", System.Net.HttpStatusCode.NotFound);
        }

        var user = await userRepository.GetUserByIdAsync(userId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

        await reactionRepository.UpsertReactionAsync(messageId, userId, emoji);

        return new ReactionEventDto(messageId, emoji, IsRemoved: false, new MessageUserDto(user.Id, user.Username, user.NameColor));
    }

    public async Task<ReactionEventDto?> RemoveReactionAsync(int messageId, int userId)
    {
        var emoji = await reactionRepository.DeleteReactionAsync(messageId, userId);
        if (emoji == null) return null;

        var user = await userRepository.GetUserByIdAsync(userId)
            ?? throw new ApiException("USER_NOT_FOUND", "User not found", System.Net.HttpStatusCode.NotFound);

        return new ReactionEventDto(messageId, emoji, IsRemoved: true, new MessageUserDto(user.Id, user.Username, user.NameColor));
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
