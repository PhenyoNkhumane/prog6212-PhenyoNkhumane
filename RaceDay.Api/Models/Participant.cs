namespace RaceDay.Api.Models;

public class Participant
{
    public int UserId { get; set; }
    public User? User { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    public ICollection<ClubMembership> Memberships { get; set; } = new List<ClubMembership>();
}
