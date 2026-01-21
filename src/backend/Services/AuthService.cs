using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Models;
using backend.Repositories;
using Microsoft.IdentityModel.Tokens;

namespace backend.Services;

public interface IAuthService
{
    Task<(bool Success, string Token, User? User)> SignInAsync(string username, string password);
    Task<(bool Success, User? User, string[] Errors)> RegisterAsync(string username, string password);
    string GenerateJwtToken(User user);
    Task<User?> ValidateTokenAsync(string token);
}

public class AuthService(
    IUserRepository userRepository,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<(bool Success, string Token, User? User)> SignInAsync(string username, string password)
    {
        logger.LogInformation("Sign-in attempt for username: {Username}", username);

        var user = await userRepository.GetUserByUsernameAsync(username);
        if (user == null)
        {
            logger.LogWarning("Sign-in failed: User not found - {Username}", username);
            return (false, string.Empty, null);
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Sign-in failed: Invalid password for user - {Username}", username);
            return (false, string.Empty, null);
        }

        await userRepository.UpdateLastLoginAsync(user.Id);
        var token = GenerateJwtToken(user);

        logger.LogInformation("Successful sign-in for user: {Username}", username);
        return (true, token, user);
    }

    public async Task<(bool Success, User? User, string[] Errors)> RegisterAsync(string username, string password)
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
            return (false, null, errors.ToArray());
        }

        var user = new User
        {
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 12)
        };

        var createdUser = await userRepository.CreateUserAsync(user);

        logger.LogInformation("Successful registration for user: {Username}", username);
        return (true, createdUser, Array.Empty<string>());
    }

    public string GenerateJwtToken(User user)
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable not configured");


        var expiryHours = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_HOURS") ?? "24");

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

    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? throw new InvalidOperationException("JWT_SECRET_KEY environment variable not configured");

            var key = Encoding.ASCII.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "nameid")
                          ?? jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                logger.LogWarning("Token validation failed: Missing user ID claim");
                return null;
            }

            if (!int.TryParse(userIdClaim.Value, out var userId))
            {
                logger.LogWarning("Token validation failed: Invalid user ID format");
                return null;
            }

            return await userRepository.GetUserByIdAsync(userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token validation failed");
            return null;
        }
    }
}
