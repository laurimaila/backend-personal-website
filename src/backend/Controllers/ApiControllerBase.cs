using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected int CurrentUserId
    {
        get
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }
    }

    protected string CurrentUsername => User.Identity?.Name ?? string.Empty;
}
