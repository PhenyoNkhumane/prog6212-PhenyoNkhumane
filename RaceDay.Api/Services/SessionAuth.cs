using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Services;

public static class SessionAuth
{
    public static void SignIn(HttpContext http, int userId, string role)
    {
        http.Session.SetInt32(SessionKeys.UserId, userId);
        http.Session.SetString(SessionKeys.Role, role);
    }

    public static void SignOut(HttpContext http) => http.Session.Clear();

    public static int? GetUserId(HttpContext http) => http.Session.GetInt32(SessionKeys.UserId);

    public static string? GetRole(HttpContext http) => http.Session.GetString(SessionKeys.Role);

    public static IActionResult? ForbidIfUnauthorized(HttpContext http, params string[] roles)
    {
        var userId = http.Session.GetInt32(SessionKeys.UserId);
        if (userId is null)
        {
            return new UnauthorizedResult();
        }

        if (roles.Length == 0)
        {
            return null;
        }

        var role = http.Session.GetString(SessionKeys.Role);
        if (string.IsNullOrWhiteSpace(role) || !roles.Contains(role))
        {
            return new StatusCodeResult(StatusCodes.Status403Forbidden);
        }

        return null;
    }
}
