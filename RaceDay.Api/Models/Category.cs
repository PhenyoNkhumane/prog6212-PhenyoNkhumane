namespace RaceDay.Api.Models;

public class Category
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int DisciplineId { get; set; }
    public required string Name { get; set; }
    public required string CategoryType { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFeeZar { get; set; }
    public int MaxSlots { get; set; }
    public TimeOnly StartTime { get; set; }

    public Event? Event { get; set; }
    public Discipline? Discipline { get; set; }
    public ICollection<Registration> Registrations { get; set; } = new List<Registration>();
}
