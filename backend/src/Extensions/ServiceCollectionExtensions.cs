﻿using backend.Repositories;
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

        services.AddDbContext<ApplicationContext>(options =>
            options.UseNpgsql(connectionString));

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
