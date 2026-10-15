namespace RaceDay.Api.Models;

public class Discipline
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
