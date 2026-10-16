namespace RaceDay.Api.Services;

public static class SessionKeys
{
    public const string UserId = "UserId";
    public const string Role = "Role";
}

public static class CurrentUser
{
    public static int? GetUserId(HttpContext http) => http.Session.GetInt32(SessionKeys.UserId);

    public static string? GetRole(HttpContext http) => http.Session.GetString(SessionKeys.Role);

    // Clear first so a new login never reuses data from an earlier session.
    public static void SignIn(HttpContext http, int userId, string role)
    {
        http.Session.Clear();
        http.Session.SetInt32(SessionKeys.UserId, userId);
        http.Session.SetString(SessionKeys.Role, role);
    }

    public static void SignOut(HttpContext http) => http.Session.Clear();
}
