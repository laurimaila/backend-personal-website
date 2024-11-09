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

public class MessageRepository(IMessagingContext context, ILogger<MessageRepository> logger) : IMessageRepository
{
    public async Task<IEnumerable<Message>> GetMessagesAsync(int limit)
    {
        var messages = await context.Messages
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages ?? Enumerable.Empty<Message>();
    }

    public async Task<Message> CreateMessageAsync(Message message)
    {
        await context.Messages.AddAsync(message);
        await context.SaveChangesAsync();
        return message;
    }

    public async Task<(IEnumerable<Message> Messages, int Total)> GetMessagesPagedAsync(
        int page,
        int pageSize)
    {
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
        // Remove all messages
        context.Messages.RemoveRange(context.Messages);
        var deleted = await context.SaveChangesAsync();
        return deleted > 0;
    }
}
