namespace RaceDay.Api.Models;

public class ClubMembership
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public int ClubId { get; set; }
    public int DisciplineId { get; set; }
    public string? MembershipNumber { get; set; }
    public DateOnly JoinedAt { get; set; }
    public required string Status { get; set; }

    public Participant? Participant { get; set; }
    public Club? Club { get; set; }
    public Discipline? Discipline { get; set; }
}
