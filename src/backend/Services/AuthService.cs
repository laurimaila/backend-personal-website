using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using backend.Configuration;
using backend.Data.Entities;
using backend.Middleware;
using backend.Repositories;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public interface IAuthService
{
    Task<(string Token, User User)> SignInAsync(string username, string password);
    Task<User> RegisterAsync(string username, string password);
    string GenerateJwtToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}

public class AuthService(
    IUserRepository userRepository,
    IOptions<ApplicationSettings> settings,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<(string Token, User User)> SignInAsync(string username, string password)
    {
        logger.LogInformation("Sign-in attempt for username: {Username}", username);

        var user = await userRepository.GetUserByUsernameAsync(username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Sign-in failed for username: {Username}", username);
            throw new ApiException("INVALID_CREDENTIALS", "Invalid username or password", System.Net.HttpStatusCode.Unauthorized);
        }

        await userRepository.UpdateLastLoginAsync(user.Id);
        var token = GenerateJwtToken(user);

        logger.LogInformation("Successful sign-in for user: {Username}", username);
        return (token, user);
    }

    public async Task<User> RegisterAsync(string username, string password)
    {
        logger.LogInformation("Registration attempt for username: {Username}", username);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
        {
            errors.Add("Username must be at least 3 characters long");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters long");
        }

        if (await userRepository.UserExistsAsync(username))
        {
            errors.Add("Username already exists");
        }

        if (errors.Any())
        {
            logger.LogWarning("Registration failed for {Username}: {Errors}", username, string.Join(", ", errors));
            throw new ApiException("REGISTRATION_FAILED", "Registration failed", errors: errors.ToArray());
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12)
        };

        var createdUser = await userRepository.CreateUserAsync(user);

        logger.LogInformation("Successful registration for user: {Username}", username);
        return createdUser;
    }

    public string GenerateJwtToken(User user)
    {
        var appSettings = settings.Value;
        var secretKey = appSettings.JwtSecretKey;
        var expiryHours = appSettings.JwtExpiryHours;

        logger.LogInformation("Creating JWT token for user {UserId} with expiry {Hours} hours", user.Id, expiryHours);

        var key = Encoding.ASCII.GetBytes(secretKey);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };

        logger.LogInformation("Adding claims: NameIdentifier={UserId}, Name={Username}", user.Id, user.Username);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        logger.LogInformation("Generated JWT token length: {Length}", tokenString.Length);
        return tokenString;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var appSettings = settings.Value;
            var secretKey = appSettings.JwtSecretKey;

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token validation failed");
            return null;
        }
    }
}
