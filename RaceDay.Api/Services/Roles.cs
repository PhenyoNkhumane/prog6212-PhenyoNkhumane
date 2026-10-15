namespace RaceDay.Api.Services;

public static class Roles
{
    public const string Participant = "Participant";
    public const string Organiser = "Organiser";

    public static readonly string[] Allowed = [Participant, Organiser];
}
