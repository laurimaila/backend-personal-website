using System;
using System.IO;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace backend.Services;

public class DatabaseInitService
{
    private readonly MessagingContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitService> _logger;

    public DatabaseInitService(
        MessagingContext dbContext,
        IConfiguration configuration,
        ILogger<DatabaseInitService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
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
            await _dbContext.Database.ExecuteSqlRawAsync($"SELECT 1 FROM {tableName} LIMIT 1");
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
            // Use init.sql file to create the tables
            string sqlFilePath = Path.Combine(AppContext.BaseDirectory, "src", "Data", "Sql", "init.sql");

            if (!File.Exists(sqlFilePath))
            {
                _logger.LogError("init.sql file not found at {Path}", sqlFilePath);
                throw new FileNotFoundException("SQL initialization file not found", sqlFilePath);
            }

            string createTableSql = await File.ReadAllTextAsync(sqlFilePath);
            _logger.LogDebug("Loaded SQL from file: {SqlFilePath}", sqlFilePath);

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
}
