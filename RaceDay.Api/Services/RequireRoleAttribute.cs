using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Services;

/// <summary>
/// Runs before the action. No session => 401 Unauthorized.
/// Logged in but role not in the allowed list => 403 Forbidden.
/// With no roles supplied it only requires a logged-in user.
/// </summary>
public class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        if (CurrentUser.GetUserId(http) is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(CurrentUser.GetRole(http)))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
        }
    }
}

/// <summary>Any logged-in user, regardless of role.</summary>
public sealed class RequireLoginAttribute : RequireRoleAttribute
{
}
