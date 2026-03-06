using System.Security.Claims;

using backend.Configuration;
using backend.Data.Entities;
using backend.DTOs;
using backend.Repositories;
using backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class UserController(
    IAuthService authService,
    IValidationService validationService,
    IUserRepository userRepository,
    IOptions<ApplicationSettings> settings,
    ILogger<UserController> logger) : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult> Register([FromBody] RegisterDto registerDto)
    {
        validationService.ValidateAndThrow(registerDto);

        var user = await authService.RegisterAsync(registerDto.Username, registerDto.Password);

        return Ok(new { user.Id, user.Username, user.CreatedAt });
    }

    [HttpPost("login")]
    public async Task<ActionResult> Login([FromBody] LoginDto loginDto)
    {
        validationService.ValidateAndThrow(loginDto);

        var (token, user) = await authService.SignInAsync(loginDto.Username, loginDto.Password);

        // Set HTTP-only cookie synced with JWT expiry
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(settings.Value.JwtExpiryHours),
            Path = "/"
        };

        Response.Cookies.Append("auth_token", token, cookieOptions);

        return Ok(new
        {
            user.Id,
            user.Username,
            user.CreatedAt,
            user.LastLogin
        });
    }

    [HttpPost("logout")]
    public ActionResult Logout()
    {
        logger.LogInformation("Logout request");

        // Clear the auth cookie
        Response.Cookies.Delete("auth_token");

        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("whoami")]
    [Authorize]
    public async Task<ActionResult> WhoAmI()
    {
        if (CurrentUserId == 0)
        {
            return Unauthorized();
        }

        var user = await userRepository.GetUserByIdAsync(CurrentUserId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            user.Id,
            user.Username,
            user.CreatedAt,
            user.LastLogin
        });
    }
}
