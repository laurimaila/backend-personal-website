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
        // Add Postgres
        services.AddDbContext<MessagingContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Register contexts
        services.AddScoped<IMessagingContext>(provider =>
            provider.GetRequiredService<MessagingContext>());

        // Register repositories
        services.AddScoped<IMessageRepository, MessageRepository>();

        // Register services
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IWebSocketService, WebSocketService>();

        return services;
    }
}
