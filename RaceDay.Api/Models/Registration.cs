namespace RaceDay.Api.Models;

public class Registration
{
    public int Id { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public required string PaymentStatus { get; set; }
    public decimal AmountPaid { get; set; }
    public string? AssignedBibNumber { get; set; }
    public bool RequiresTempAsaLicense { get; set; }
    public string? MedicalAidName { get; set; }
    public string? MedicalAidNumber { get; set; }
    public required string Status { get; set; }

    public Participant? Participant { get; set; }
    public Category? Category { get; set; }
    public Result? Result { get; set; }
}
