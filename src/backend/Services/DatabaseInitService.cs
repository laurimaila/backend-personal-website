using backend.Configuration;
using backend.Data;
using backend.Data.Entities;
using backend.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace backend.Services;

public class DatabaseInitService(
    ApplicationContext dbContext,
    IServiceProvider serviceProvider,
    ILogger<DatabaseInitService> logger,
    IOptions<ApplicationSettings> settings)
{
    private readonly ApplicationSettings _settings = settings.Value;

    public async Task InitializeAsync()
    {
        try
        {
            logger.LogInformation("Starting database initialization");

            await dbContext.Database.MigrateAsync();

            await CreateDefaultVisitorUserAsync();
            await PromoteAdminUserAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private async Task CreateDefaultVisitorUserAsync()
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var existingVisitor = await userRepository.GetUserByUsernameAsync("Visitor");
            if (existingVisitor != null)
            {
                logger.LogInformation("Visitor user already exists, skipping creation");
                return;
            }

            var visitorUser = new User
            {
                Username = "Visitor",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("VisitorPassword", 12),
                CreatedAt = DateTime.UtcNow
            };

            await userRepository.CreateUserAsync(visitorUser);
            logger.LogInformation("Default Visitor user created successfully with ID: {UserId}", visitorUser.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create default Visitor user");
            throw;
        }
    }

    private async Task PromoteAdminUserAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.AdminUsername))
            return;

        using var scope = serviceProvider.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepository.GetUserByUsernameAsync(_settings.AdminUsername);
        if (user == null)
        {
            logger.LogWarning("ADMIN_USERNAME '{Username}' not found, skipping admin promotion", _settings.AdminUsername);
            return;
        }

        if (user.IsAdmin)
        {
            logger.LogInformation("User '{Username}' is already admin", _settings.AdminUsername);
            return;
        }

        user.IsAdmin = true;
        await userRepository.UpdateUserAsync(user);
        logger.LogInformation("User '{Username}' promoted to admin", _settings.AdminUsername);
    }
}
