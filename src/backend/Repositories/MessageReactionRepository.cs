using backend.Data;
using backend.Data.Entities;

using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public interface IMessageReactionRepository
{
    Task<MessageReaction?> GetReactionAsync(int messageId, int userId);
    Task UpsertReactionAsync(int messageId, int userId, string emoji);
    Task<string?> DeleteReactionAsync(int messageId, int userId);
}

public class MessageReactionRepository(IApplicationContext context, ILogger<MessageReactionRepository> logger)
    : IMessageReactionRepository
{
    public async Task<MessageReaction?> GetReactionAsync(int messageId, int userId)
    {
        return await context.MessageReactions
            .FirstOrDefaultAsync(r => r.MessageId == messageId && r.UserId == userId);
    }

    public async Task UpsertReactionAsync(int messageId, int userId, string emoji)
    {
        var existing = await GetReactionAsync(messageId, userId);

        if (existing != null)
        {
            logger.LogInformation("Updating reaction for user {UserId} on message {MessageId}", userId, messageId);
            existing.Emoji = emoji;
        }
        else
        {
            logger.LogInformation("Adding reaction for user {UserId} on message {MessageId}", userId, messageId);
            await context.MessageReactions.AddAsync(new MessageReaction
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji
            });
        }

        await context.SaveChangesAsync();
    }

    public async Task<string?> DeleteReactionAsync(int messageId, int userId)
    {
        var reaction = await GetReactionAsync(messageId, userId);
        if (reaction == null) return null;

        logger.LogInformation("Removing reaction for user {UserId} on message {MessageId}", userId, messageId);
        context.MessageReactions.Remove(reaction);
        await context.SaveChangesAsync();
        return reaction.Emoji;
    }
}
