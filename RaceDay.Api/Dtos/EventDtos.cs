namespace RaceDay.Api.Dtos;

public class CreateEventDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateOnly Date { get; set; }
    public required string VenueName { get; set; }
    public required string City { get; set; }
    public required string Province { get; set; }
    public decimal DistanceKm { get; set; }
    public required string EventType { get; set; }
    public required string Status { get; set; }
}

public class UpdateEventDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateOnly? Date { get; set; }
    public string? VenueName { get; set; }
    public string? City { get; set; }
    public string? Province { get; set; }
    public decimal? DistanceKm { get; set; }
    public string? EventType { get; set; }
    public string? Status { get; set; }
}

public class CreateCategoryDto
{
    public int DisciplineId { get; set; }
    public required string Name { get; set; }
    public required string CategoryType { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal EntryFeeZar { get; set; }
    public int MaxSlots { get; set; }
    public TimeOnly StartTime { get; set; }
}

public class UpdateCategoryDto
{
    public string? Name { get; set; }
    public string? CategoryType { get; set; }
    public decimal? DistanceKm { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? EntryFeeZar { get; set; }
    public int? MaxSlots { get; set; }
    public TimeOnly? StartTime { get; set; }
}

public class EnrolDto
{
    public int CategoryId { get; set; }
    public bool RequiresTempAsaLicense { get; set; }
    public string? MedicalAidName { get; set; }
    public string? MedicalAidNumber { get; set; }
}

public class UpdateRegistrationDto
{
    public string? PaymentStatus { get; set; }
    public decimal? AmountPaid { get; set; }
    public string? AssignedBibNumber { get; set; }
    public string? Status { get; set; }
    public string? MedicalAidName { get; set; }
    public string? MedicalAidNumber { get; set; }
}

public class CreateResultDto
{
    public int RegistrationId { get; set; }
    public TimeOnly? GunTime { get; set; }
    public TimeOnly? NetTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public required string Status { get; set; }
}

public class UpdateResultDto
{
    public TimeOnly? GunTime { get; set; }
    public TimeOnly? NetTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public string? Status { get; set; }
}

public class CreateDisciplineDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
}

public class CreateClubDto
{
    public required string Name { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactNumber { get; set; }
}

public class CreateMembershipDto
{
    public int DisciplineId { get; set; }
    public string? MembershipNumber { get; set; }
    public DateOnly JoinedAt { get; set; }
}
