using backend.Attributes;
using backend.Data.Entities;
using backend.DTOs;
using backend.Services;

using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public class UserController(IAuthService authService, IValidationService validationService, ILogger<UserController> logger) : ControllerBase
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
