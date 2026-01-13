using System;
using System.IO;
using System.Threading.Tasks;
using backend.Models;
using backend.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Services;

public class DatabaseInitService
{
    private readonly ApplicationContext _dbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitService> _logger;

    public DatabaseInitService(
        ApplicationContext dbContext,
        IServiceProvider serviceProvider,
        ILogger<DatabaseInitService> logger)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization");

            // Add a visitor user by default
            await CreateDefaultVisitorUserAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private async Task CreateDefaultVisitorUserAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Check if visitor user exists
            var existingVisitor = await userRepository.GetUserByUsernameAsync("Visitor");
            if (existingVisitor != null)
            {
                _logger.LogInformation("Visitor user already exists, skipping creation");
                return;
            }

            // Create visitor user
            var visitorUser = new User
            {
                Username = "Visitor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("VisitorPassword", 12),
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.CreateUserAsync(visitorUser);
            _logger.LogInformation("Default Visitor user created successfully with ID: {UserId}", visitorUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create default Visitor user");
            throw;
        }
    }
}
