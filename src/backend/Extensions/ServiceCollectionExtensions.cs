using backend.Configuration;
using backend.Data;
using backend.Repositories;
using backend.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<ApplicationSettings>()
            .Bind(configuration)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register gRPC Client
        services.AddGrpcClient<Backend.Protos.PhysicsService.PhysicsServiceClient>((sp, o) =>
        {
            var settings = sp.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            o.Address = new Uri(settings.PhysicsSvcUrl);
        });

        services.AddDbContext<ApplicationContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            // Use CONNECTION_STRING if provided, otherwise construct from individual settings
            var connectionString = !string.IsNullOrWhiteSpace(settings.ConnectionString)
                ? settings.ConnectionString
                : $"Host={settings.PostgresHost};Database={settings.PostgresDb};Username={settings.PostgresUser};Password={settings.PostgresPassword}";

            options.UseNpgsql(connectionString);
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
