using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public interface IMessageRepository
{
    Task<IEnumerable<Message>> GetMessagesAsync(int limit);
    Task<Message> CreateMessageAsync(Message message);

    Task<(IEnumerable<Message> Messages, int Total)> GetMessagesPagedAsync(int page, int pageSize);
    Task<bool> DeleteAllMessagesAsync();
}

public class MessageRepository(IApplicationContext context, ILogger<MessageRepository> logger) : IMessageRepository
{
    public async Task<IEnumerable<Message>> GetMessagesAsync(int limit)
    {
        logger.LogInformation("Fetching {Limit} messages", limit);
        var messages = await context.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        logger.LogInformation("Successfully fetched {Count} messages", messages.Count());
        return messages ?? Enumerable.Empty<Message>();
    }

    public async Task<Message> CreateMessageAsync(Message message)
    {
        await context.Messages.AddAsync(message);
        await context.SaveChangesAsync();
        logger.LogInformation("Successfully created message {MessageId} from {CreatorName}", message.Id, message.Creator);
        return message;
    }

    public async Task<(IEnumerable<Message> Messages, int Total)> GetMessagesPagedAsync(
        int page,
        int pageSize)
    {
        logger.LogInformation("Fetching page {Page} with page size {PageSize}", page, pageSize);
        var query = context.Messages.OrderByDescending(m => m.CreatedAt);
        var total = await query.CountAsync();

        var messages = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return (messages, total);
    }

    public async Task<bool> DeleteAllMessagesAsync()
    {
        logger.LogWarning("Deleting all chat messages");
        context.Messages.RemoveRange(context.Messages);
        var deleted = await context.SaveChangesAsync();
        return deleted > 0;
    }
}
