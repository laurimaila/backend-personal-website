using Microsoft.AspNetCore.Mvc;
using backend.Models;
using backend.DTOs;
using backend.Services;
using backend.Attributes;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class UserController(IAuthService authService, ILogger<UserController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            logger.LogInformation("Registration attempt for username: {Username}", registerDto.Username);

            var (success, user, errors) = await authService.RegisterAsync(registerDto.Username, registerDto.Password);

            if (!success)
            {
                logger.LogWarning("Registration failed for {Username}: {Errors}", registerDto.Username, string.Join(", ", errors));
                return BadRequest(new { message = "Registration failed", errors });
            }

            logger.LogInformation("Successful registration for username: {Username}", registerDto.Username);
            return Ok(new { user!.Id, user.Username, user.CreatedAt });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during registration for username: {Username}", registerDto.Username);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            logger.LogInformation("Login attempt for username: {Username}", loginDto.Username);
            var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            var (success, token, user) = await authService.SignInAsync(loginDto.Username, loginDto.Password);

            if (!success)
            {
                logger.LogWarning("Login failed for username: {Username}", loginDto.Username);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Set HTTP-only cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(24),
                Path = "/"
            };

            Response.Cookies.Append("auth_token", token, cookieOptions);

            return Ok(new
            {
                user!.Id,
                user.Username,
                user.CreatedAt,
                user.LastLogin
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during login for username: {Username}", loginDto.Username);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        try
        {
            logger.LogInformation("Logout request");

            // Clear the auth cookie
            Response.Cookies.Delete("auth_token");

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("whoami")]
    [RequireAuth]
    public ActionResult WhoAmI()
    {
        var user = HttpContext.Items["AuthenticatedUser"] as User;

        return Ok(new
        {
            user!.Id,
            user.Username,
            user.CreatedAt,
            user.LastLogin
        });
    }
}
