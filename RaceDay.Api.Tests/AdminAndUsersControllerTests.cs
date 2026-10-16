using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AdminAndUsersControllerTests
{
    [Fact]
    public void GetMe_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new UsersController(db);
        ControllerTestHelper.AttachUser(controller);

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
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);

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
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

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
        ControllerTestHelper.AttachUser(controller, organiser.Id, Roles.Organiser);

        // Act
        var result = controller.CreateDiscipline(new CreateDisciplineDto { Name = "Running" });

        // Assert
        Assert.IsType<ConflictObjectResult>(result);
        Assert.Single(db.Disciplines);
    }

}
