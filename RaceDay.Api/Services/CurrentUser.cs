using System.Security.Claims;

namespace RaceDay.Api.Services;

public static class CurrentUser
{
    public static int? GetUserId(HttpContext http) =>
        int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;

    public static string? GetRole(HttpContext http) => http.User.FindFirstValue(ClaimTypes.Role);
}
