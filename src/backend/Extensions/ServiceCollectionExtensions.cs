using backend.Models;
using backend.Repositories;
using backend.Services;
using Microsoft.EntityFrameworkCore;

namespace backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "";

        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(connectionString));

        // Register gRPC Client
        services.AddGrpcClient<Backend.Protos.PhysicsService.PhysicsServiceClient>(o =>
        {
            o.Address = new Uri(Environment.GetEnvironmentVariable("GO_PHYS_SVC_URL") ?? "");
        });

        // Register contexts
        services.AddScoped<IApplicationContext>(provider =>
            provider.GetRequiredService<ApplicationContext>());

        // Register repositories
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Register services
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IWebSocketService, WebSocketService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<DatabaseInitService>();

        return services;
    }
}
