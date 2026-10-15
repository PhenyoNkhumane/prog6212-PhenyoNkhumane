using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public void Register_NewParticipant_ReturnsCreatedAndSavesUser()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new AuthController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Secret123!",
            FirstName = "Phenyo",
            LastName = "Nkhumane",
            IdNumberOrPassport = "9001015009089"
        };

        // Act
        var result = controller.Register(dto);

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Users);
        Assert.Single(db.Participants);
        Assert.Equal(Roles.Participant, db.Users.Single().Role);
        Assert.NotEqual("Secret123!", db.Users.Single().PasswordHash);
    }

    [Fact]
    public void Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new AuthController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Another123!",
            FirstName = "Naledi",
            LastName = "Mokoena",
            IdNumberOrPassport = "9202026007090"
        };

        // Act
        var result = controller.Register(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.Users);
    }

    [Fact]
    public void Login_ValidCredentials_ReturnsOkAndSetsSession()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new AuthController(db);
        ControllerTestHelper.AttachSession(controller);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(user.Id, controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
        Assert.Equal(Roles.Participant, controller.HttpContext.Session.GetString(SessionKeys.Role));
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret123!", json);
    }

    [Fact]
    public void Login_UnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new AuthController(db);
        ControllerTestHelper.AttachSession(controller);
        var dto = new LoginDto { Email = "missing@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
    }

    [Fact]
    public void Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new AuthController(db);
        ControllerTestHelper.AttachSession(controller);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "WrongPassword" };

        // Act
        var result = controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
        Assert.Null(controller.HttpContext.Session.GetInt32(SessionKeys.UserId));
    }

    [Fact]
    public void ChangePassword_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = new AuthController(db);
        ControllerTestHelper.AttachSession(controller);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = controller.ChangePassword(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void ChangePassword_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = new AuthController(db);
        ControllerTestHelper.AttachSession(controller, user.Id, Roles.Participant);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = controller.ChangePassword(dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var login = controller.Login(new LoginDto { Email = "runner@raceday.co.za", Password = "NewSecret123!" });
        Assert.IsType<OkObjectResult>(login);
    }
}
