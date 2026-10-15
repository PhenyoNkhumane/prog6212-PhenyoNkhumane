namespace RaceDay.Api.Models;

public class Organiser
{
    public int UserId { get; set; }
    public required string ProvisionedBy { get; set; }
    public DateTime ProvisionedAt { get; set; } = DateTime.UtcNow;
    public User? User { get; set; }
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
