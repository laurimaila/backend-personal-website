using backend.Repositories;
using backend.Services;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
                               configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<MessagingContext>(options =>
            options.UseNpgsql(connectionString));

        // Register contexts
        services.AddScoped<IMessagingContext>(provider =>
            provider.GetRequiredService<MessagingContext>());

        // Register repositories
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Register services
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IWebSocketService, WebSocketService>();
        services.AddScoped<DatabaseInitService>();

        return services;
    }
}
