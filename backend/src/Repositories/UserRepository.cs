using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<User?> GetUserByIdAsync(int id);
    Task<User> CreateUserAsync(User user);
    Task<User> UpdateUserAsync(User user);
    Task<bool> UserExistsAsync(string username);
    Task UpdateLastLoginAsync(int userId);
}

public class UserRepository(IApplicationContext context, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        logger.LogInformation("Fetching user by username: {Username}", username);

        var user = await context.Users
            .Where(u => u.Username == username)
            .FirstOrDefaultAsync();

        logger.LogInformation("User {Username} found: {Found}", username, user != null);
        return user;
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        logger.LogInformation("Fetching user by ID: {UserId}", id);

        var user = await context.Users
            .Where(u => u.Id == id)
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<User> CreateUserAsync(User user)
    {
        logger.LogInformation("Creating new user: {Username}", user.Username);

        user.CreatedAt = DateTime.UtcNow;

        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Successfully created user {UserId} with username {Username}", user.Id, user.Username);
        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        logger.LogInformation("Updating user: {UserId}", user.Id);

        context.Users.Update(user);
        await context.SaveChangesAsync();

        logger.LogInformation("Successfully updated user {UserId}", user.Id);
        return user;
    }

    public async Task<bool> UserExistsAsync(string username)
    {
        logger.LogInformation("Checking if user exists: {Username}", username);

        var exists = await context.Users
            .AnyAsync(u => u.Username == username);

        logger.LogInformation("User {Username} exists: {Exists}", username, exists);
        return exists;
    }
    
    public async Task UpdateLastLoginAsync(int userId)
    {
        logger.LogInformation("Updating last login for user: {UserId}", userId);
        
        var user = await context.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLogin = DateTime.UtcNow;
            await context.SaveChangesAsync();
            
            logger.LogInformation("Updated last login for user {UserId}", userId);
        }
    }
}
