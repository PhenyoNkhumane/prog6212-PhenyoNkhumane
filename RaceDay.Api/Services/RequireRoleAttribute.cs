using Microsoft.AspNetCore.Authorization;

namespace RaceDay.Api.Services;

public sealed class RequireRoleAttribute : AuthorizeAttribute
{
    public RequireRoleAttribute(params string[] roles)
    {
        Roles = string.Join(",", roles);
    }
}
