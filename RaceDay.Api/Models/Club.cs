namespace RaceDay.Api.Models;

public class Club
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
