using backend.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

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
        var path = context.Request.Path.Value;

        if (context.Request.Cookies.TryGetValue("auth_token", out var token) && !string.IsNullOrEmpty(token))
        {
            var principal = authService.ValidateToken(token);
            if (principal != null)
            {
                context.User = principal;
                _logger.LogDebug("Authenticated user {Username} for request {Path}", principal.Identity?.Name, path);
            }
            else
            {
                context.Response.Cookies.Delete("auth_token");
            }
        }

        await _next(context);
    }
}

// Throw custom ApiException on authorization Forbidden or Challenged
public class ApiExceptionAuthorizationHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult result)
    {
        if (result.Forbidden)
        {
            throw new ApiException("FORBIDDEN", "You do not have permission to access this resource", System.Net.HttpStatusCode.Forbidden);
        }

        if (result.Challenged)
        {
            throw new ApiException("UNAUTHORIZED", "Authentication required", System.Net.HttpStatusCode.Unauthorized);
        }

        await _defaultHandler.HandleAsync(next, context, policy, result);
    }
}

public static class AuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}
