namespace RaceDay.Api.Models;

public class Result
{
    public int Id { get; set; }
    public int RegistrationId { get; set; }
    public TimeOnly? GunTime { get; set; }
    public TimeOnly? NetTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public required string Status { get; set; }

    public Registration? Registration { get; set; }
}
