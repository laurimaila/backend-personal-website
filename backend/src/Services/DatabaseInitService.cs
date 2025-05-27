using System;
using System.Threading.Tasks;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace backend.Services;

public class DatabaseInitService
{
    private readonly MessagingContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitService> _logger;
    private readonly string _connectionString;

    public DatabaseInitService(
        MessagingContext dbContext,
        IConfiguration configuration,
        ILogger<DatabaseInitService> logger)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _logger = logger;
        _connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ??
            configuration.GetConnectionString("DefaultConnection");
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database init");

            await _dbContext.Database.EnsureCreatedAsync();

            // Check if the messages table exists
            bool tableExists = await TableExistsAsync("messages");

            if (!tableExists)
            {
                _logger.LogWarning("Messages table not found, creating the table");
                await CreateMessagesTableAsync();
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
            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT EXISTS (SELECT FROM information_schema.tables WHERE table_schema = 'public' AND table_name = @tableName)",
                connection);

            command.Parameters.AddWithValue("tableName", tableName);

            _logger.LogInformation("Checking if table {TableName} exists", tableName);
            var result = await command.ExecuteScalarAsync();
            var exists = result != null && (bool)result;
            _logger.LogInformation("Table {TableName} exists: {Exists}", tableName, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if table {TableName} exists", tableName);
            return false;
        }
    }

    private async Task CreateMessagesTableAsync()
    {
        try
        {
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS messages (
                    id SERIAL PRIMARY KEY,
                    content TEXT NOT NULL,
                    creator_name TEXT NOT NULL, 
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(createTableSql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating messages table", ex);
            throw;
        }
    }
}
