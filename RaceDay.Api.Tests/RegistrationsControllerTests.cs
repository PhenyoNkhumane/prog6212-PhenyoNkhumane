using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class RegistrationsControllerTests
{
    [Fact]
    public async Task Enrol_AsParticipant_SavesRegistration()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var (participant, eventId, categoryId) = SeedEventWithCategory(db);
        var controller = new RegistrationsController(db);
        ControllerTestHelper.AttachUser(controller, participant.Id, Roles.Participant);

        // Act
        var result = await controller.Enrol(eventId, new EnrolDto { CategoryId = categoryId });

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Registrations);
        Assert.Equal(participant.Id, db.Registrations.Single().ParticipantId);
    }

    [Fact]
    public async Task Enrol_PreviouslyCancelledCategory_AllowsReenrolment()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var (participant, eventId, categoryId) = SeedEventWithCategory(db);
        db.Registrations.Add(new Registration
        {
            ParticipantId = participant.Id,
            CategoryId = categoryId,
            PaymentStatus = "Pending",
            Status = "Cancelled"
        });
        db.SaveChanges();
        var controller = new RegistrationsController(db);
        ControllerTestHelper.AttachUser(controller, participant.Id, Roles.Participant);

        // Act
        var result = await controller.Enrol(eventId, new EnrolDto { CategoryId = categoryId });

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Equal(2, db.Registrations.Count());
    }

    [Fact]
    public async Task Enrol_UnknownEvent_ReturnsNotFound()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var participant = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new RegistrationsController(db);
        ControllerTestHelper.AttachUser(controller, participant.Id, Roles.Participant);

        // Act
        var result = await controller.Enrol(99, new EnrolDto { CategoryId = 1 });

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Enrol_DraftEvent_ReturnsConflict()
    {
        using var db = TestDbFactory.Create();
        var (participant, eventId, categoryId) = SeedEventWithCategory(db);
        db.Events.Single().Status = "Draft";
        db.SaveChanges();
        var controller = new RegistrationsController(db);
        ControllerTestHelper.AttachUser(controller, participant.Id, Roles.Participant);

        var result = await controller.Enrol(eventId, new EnrolDto { CategoryId = categoryId });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(db.Registrations);
    }

    [Fact]
    public async Task Enrol_AgeCategoryRejectsParticipantOutsideAgeRange()
    {
        using var db = TestDbFactory.Create();
        var (participant, eventId, categoryId) = SeedEventWithCategory(db);
        participant.IdNumberOrPassport = "0001010000000";
        var category = db.Categories.Single();
        category.CategoryType = "Age";
        category.DistanceKm = null;
        category.MinAge = 60;
        category.MaxAge = 120;
        db.SaveChanges();
        var controller = new RegistrationsController(db);
        ControllerTestHelper.AttachUser(controller, participant.Id, Roles.Participant);

        var result = await controller.Enrol(eventId, new EnrolDto { CategoryId = categoryId });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Registrations);
    }

    private static (User Participant, int EventId, int CategoryId) SeedEventWithCategory(RaceDay.Api.Data.ApplicationDbContext db)
    {
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var participant = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var discipline = new Discipline { Name = "Running" };
        db.Disciplines.Add(discipline);
        db.SaveChanges();

        var ev = new Event
        {
            OrganiserId = organiser.Id,
            Title = "Johannesburg Road Race",
            Date = new DateOnly(2027, 10, 10),
            DistanceKm = 21,
            EventType = "Run",
            VenueName = "Zoo Lake",
            City = "Johannesburg",
            Province = "Gauteng",
            Status = "Published"
        };
        db.Events.Add(ev);
        db.SaveChanges();

        var category = new Category
        {
            EventId = ev.Id,
            DisciplineId = discipline.Id,
            Name = "21km Half Marathon",
            CategoryType = "Distance",
            DistanceKm = 21,
            EntryFeeZar = 250,
            MaxSlots = 800,
            StartTime = new TimeOnly(6, 30)
        };
        db.Categories.Add(category);
        db.SaveChanges();
        return (participant, ev.Id, category.Id);
    }
}
