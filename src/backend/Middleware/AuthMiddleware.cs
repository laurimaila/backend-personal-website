using backend.Models;
using backend.Services;

namespace backend.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        // Skip authentication for certain paths
        var path = context.Request.Path.Value?.ToLower();
        var skipAuth = path?.StartsWith("/api/auth/login") == true ||
                   path?.StartsWith("/api/auth/register") == true ||
                   path?.StartsWith("/api/auth/logout") == true ||
                   path?.StartsWith("/api/auth/check") == true ||
                   path?.StartsWith("/health") == true ||
                   path?.StartsWith("/swagger") == true;

        if (!skipAuth && context.Request.Cookies.TryGetValue("auth_token", out var token) && !string.IsNullOrEmpty(token))
        {
            try
            {
                var user = await authService.ValidateTokenAsync(token);
                if (user != null)
                {
                    context.Items["User"] = user;
                    context.Items["AuthenticatedUser"] = user;
                    _logger.LogDebug("Authenticated user {Username} for request {Path}", user.Username, path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed for request {Path}", path);
                context.Response.Cookies.Delete("auth_token");
            }
        }

        await _next(context);
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
