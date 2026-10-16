using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Tests;

public class ReadModelResponseTests
{
    [Fact]
    public void IdentityNumber_IsEncryptedInStorageAndDecryptedWhenRead()
    {
        using var db = TestDbFactory.Create();
        var participant = ControllerTestHelper.AddParticipant(db, "runner@example.test", "test-password");
        participant.IdNumberOrPassport = "0001010000000";
        db.SaveChanges();

        var storedValue = db.Database.SqlQueryRaw<string>(
            "SELECT id_number_or_passport AS Value FROM [User] WHERE id = {0}",
            participant.Id).Single();

        Assert.NotEqual(participant.IdNumberOrPassport, storedValue);
        Assert.StartsWith("v1:", storedValue);

        db.Entry(participant).State = EntityState.Detached;
        var reloaded = db.Users.Single(user => user.Id == participant.Id);
        Assert.Equal("0001010000000", reloaded.IdNumberOrPassport);
    }

    [Fact]
    public void GetEvents_HidesDraftAndCancelledEvents()
    {
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "owner@example.test", "test-password");
        db.Events.AddRange(
            CreateEvent(organiser.Id, "Published", "Visible"),
            CreateEvent(organiser.Id, "Draft", "Hidden draft"),
            CreateEvent(organiser.Id, "Cancelled", "Hidden cancelled"));
        db.SaveChanges();

        var result = Assert.IsType<OkObjectResult>(new EventsController(db).GetEvents());
        var events = Assert.IsAssignableFrom<IEnumerable<EventResponseDto>>(result.Value).ToList();

        Assert.Single(events);
        Assert.Equal("Visible", events[0].Title);
    }

    [Fact]
    public void GetEventResults_ReturnsDtoWithoutRegistrationOrMedicalAidData()
    {
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "owner@example.test", "test-password");
        var participant = ControllerTestHelper.AddParticipant(db, "runner@example.test", "test-password");
        var discipline = new Discipline { Name = "Running" };
        db.Disciplines.Add(discipline);
        db.SaveChanges();

        var ev = new Event
        {
            OrganiserId = organiser.Id,
            Title = "Sample Race",
            Date = new DateOnly(2027, 10, 10),
            DistanceKm = 10,
            EventType = "Run",
            VenueName = "Test Park",
            City = "Sample City",
            Province = "Sample Province",
            Status = "Published"
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var category = new Category
        {
            EventId = ev.Id,
            DisciplineId = discipline.Id,
            Name = "10km",
            CategoryType = "Distance",
            DistanceKm = 10,
            EntryFeeZar = 100,
            MaxSlots = 20,
            StartTime = new TimeOnly(8, 0)
        };
        db.Categories.Add(category);
        db.SaveChanges();

        var registration = new Registration
        {
            ParticipantId = participant.Id,
            CategoryId = category.Id,
            PaymentStatus = "Paid",
            AmountPaid = 100,
            MedicalAidName = "Private Provider",
            MedicalAidNumber = "PRIVATE-123",
            Status = "Active"
        };
        db.Registrations.Add(registration);
        db.SaveChanges();
        db.Results.Add(new Result
        {
            RegistrationId = registration.Id,
            GunTime = new TimeOnly(1, 0),
            NetTime = new TimeOnly(0, 59),
            OverallPosition = 1,
            CategoryPosition = 1,
            Status = "Finished"
        });
        db.SaveChanges();

        var controller = new ResultsController(db);
        var result = Assert.IsType<OkObjectResult>(controller.GetForEvent(ev.Id));
        var response = Assert.IsAssignableFrom<IEnumerable<ResultResponseDto>>(result.Value).Single();
        Assert.Equal(category.Id, response.CategoryId);

        var json = JsonSerializer.Serialize(response);
        Assert.DoesNotContain("ParticipantId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrationId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MedicalAid", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PRIVATE-123", json);
    }

    [Fact]
    public void GetCategories_DraftEventIsNotPublic()
    {
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "owner@example.test", "test-password");
        var ev = new Event
        {
            OrganiserId = organiser.Id,
            Title = "Draft Race",
            Date = new DateOnly(2027, 10, 10),
            DistanceKm = 10,
            EventType = "Run",
            VenueName = "Test Park",
            City = "Sample City",
            Province = "Sample Province",
            Status = "Draft"
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var result = new CategoriesController(db).GetForEvent(ev.Id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void GetClubs_ReturnsDedicatedDto()
    {
        using var db = TestDbFactory.Create();
        db.Clubs.Add(new Club { Name = "Example Club", CreatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var result = Assert.IsType<OkObjectResult>(new ClubsController(db).GetClubs());

        Assert.IsAssignableFrom<IEnumerable<ClubResponseDto>>(result.Value);
    }

    private static Event CreateEvent(int organiserId, string status, string title) => new()
    {
        OrganiserId = organiserId,
        Title = title,
        Date = new DateOnly(2027, 10, 10),
        DistanceKm = 10,
        EventType = "Run",
        VenueName = "Test Park",
        City = "Sample City",
        Province = "Sample Province",
        Status = status
    };
}
