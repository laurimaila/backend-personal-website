using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace backend.Attributes;

public class RequireAuthAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // Check for [AllowAnonymous]
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata
            .Any(m => m is AllowAnonymousAttribute);

        if (allowAnonymous)
        {
            return;
        }

        var user = context.HttpContext.Items["User"] as User;
        if (user == null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Authentication required" });
            return;
        }

        // Add user to context for access in controllers
        context.HttpContext.Items["AuthenticatedUser"] = user;
    }
}
