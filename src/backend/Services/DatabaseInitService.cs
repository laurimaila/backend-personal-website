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

            // Ensure the database exists
            await _dbContext.Database.EnsureCreatedAsync();

            // Check if the messages table exists
            bool tableExists = await TableExistsAsync("messages");

            if (!tableExists)
            {
                _logger.LogWarning("Messages table not found, creating the table");
                await CreateTablesAsync();
                _logger.LogInformation("Messages table created successfully");
            }
            else
            {
                _logger.LogInformation("Messages table already exists");
            }

            // Add default visitor user
            await CreateDefaultVisitorUserAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    private async Task<bool> TableExistsAsync(string tableName)
    {
        try
        {
            var result = await _dbContext.Database.ExecuteSqlAsync(
                $"SELECT 1 FROM {tableName:raw} LIMIT 1");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task CreateTablesAsync()
    {
        try
        {
            string initSqlFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "Sql", "init.sql");

            if (!File.Exists(initSqlFilePath))
            {
                _logger.LogError("init.sql file not found at {Path}", initSqlFilePath);
                throw new FileNotFoundException("SQL initialization file not found", initSqlFilePath);
            }

            string createTableSql = await File.ReadAllTextAsync(initSqlFilePath);
            _logger.LogDebug("Loaded SQL from file: {initSqlFilePath}", initSqlFilePath);

            // Execute raw SQL using EF Core
            await _dbContext.Database.ExecuteSqlRawAsync(createTableSql);
            _logger.LogInformation("Successfully executed init.sql");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tables from init.sql");
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
