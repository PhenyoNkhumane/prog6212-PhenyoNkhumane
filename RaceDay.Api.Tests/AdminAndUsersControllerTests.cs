using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AdminAndUsersControllerTests
{
    [Fact]
    public void CreateOrganiser_MissingAdminKey_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new AdminController(db, Config("admin-secret"));
        ControllerTestHelper.AttachSession(controller);
        var dto = ValidOrganiser();

        // Act
        var result = controller.CreateOrganiser(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public void CreateOrganiser_ValidKey_SavesOrganiserAndRequiresPasswordChange()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new AdminController(db, Config("admin-secret"));
        ControllerTestHelper.AttachSession(controller);
        controller.HttpContext.Request.Headers["X-Admin-Key"] = "admin-secret";

        // Act
        var result = controller.CreateOrganiser(ValidOrganiser());

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Users);
        Assert.Single(db.Organisers);
        Assert.Equal(Roles.Organiser, db.Users.Single().Role);
        Assert.True(db.Users.Single().MustChangePassword);
        Assert.DoesNotContain("HASH", db.Users.Single().PasswordHash);
    }

    [Fact]
    public void GetMe_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new UsersController(db);
        ControllerTestHelper.AttachSession(controller);

        // Act
        var result = controller.GetMe();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetMe_WithSession_ReturnsOkWithoutPasswordHash()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new UsersController(db);
        ControllerTestHelper.AttachSession(controller, user.Id, Roles.Participant);

        // Act
        var result = controller.GetMe();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.Contains("runner@raceday.co.za", json);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret123!", json);
    }

    [Fact]
    public void CreateDiscipline_AsOrganiser_SavesDiscipline()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        var controller = new DisciplinesController(db);
        ControllerTestHelper.AttachSession(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateDiscipline(new CreateDisciplineDto { Name = "Running" });

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Disciplines);
    }

    [Fact]
    public void CreateDiscipline_DuplicateName_ReturnsConflict()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var organiser = ControllerTestHelper.AddOrganiser(db, "org@raceday.co.za", "Secret123!");
        db.Disciplines.Add(new RaceDay.Api.Models.Discipline { Name = "Running" });
        db.SaveChanges();
        var controller = new DisciplinesController(db);
        ControllerTestHelper.AttachSession(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateDiscipline(new CreateDisciplineDto { Name = "Running" });

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Disciplines);
    }

    private static IConfiguration Config(string key) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AdminProvisioningKey"] = key
            })
            .Build();

    private static CreateOrganiserDto ValidOrganiser() => new()
    {
        Email = "organiser@raceday.co.za",
        FirstName = "Thabo",
        LastName = "Mokoena",
        IdNumberOrPassport = "8001015009087"
    };
}
