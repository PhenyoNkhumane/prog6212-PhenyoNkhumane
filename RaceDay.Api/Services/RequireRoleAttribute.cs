using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Services;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public RequireRoleAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var result = SessionAuth.ForbidIfUnauthorized(context.HttpContext, _roles);
        if (result is not null)
        {
            context.Result = result;
        }
    }
}
