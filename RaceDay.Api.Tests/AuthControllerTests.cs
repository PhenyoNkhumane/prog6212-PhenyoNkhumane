using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RaceDay.Api.Controllers;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_NewParticipant_ReturnsCreatedAndSavesUser()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Secret123!xyz",
            FirstName = "Taylor",
            LastName = "Runner",
            IdNumberOrPassport = "TEST-ID-001",
            Role = Roles.Participant
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Users);
        Assert.Single(db.Participants);
        Assert.Equal(Roles.Participant, db.Users.Single().Role);
        Assert.NotEqual("Secret123!", db.Users.Single().PasswordHash);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        var dto = new RegisterDto
        {
            Email = "runner@raceday.co.za",
            Password = "Another123!xyz",
            FirstName = "Naledi",
            LastName = "Mokoena",
            IdNumberOrPassport = "TEST-ID-002",
            Role = Roles.Participant
        };

        // Act
        var result = await controller.Register(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(db.Users);
    }

    [Fact]
    public async Task Register_Organiser_CreatesOrganiserSubtypeAndJwt()
    {
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var result = await controller.Register(new RegisterDto
        {
            Email = "organiser@example.test",
            Password = "StrongPassword123!",
            FirstName = "Alex",
            LastName = "Example",
            IdNumberOrPassport = "TEST-ORG-ID",
            Role = Roles.Organiser
        });

        Assert.IsType<CreatedResult>(result);
        Assert.Single(db.Organisers);
        Assert.Empty(db.Participants);
        Assert.Equal(Roles.Organiser, db.Users.Single().Role);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsBadRequest()
    {
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        var result = await controller.Register(new RegisterDto
        {
            Email = "runner@example.test",
            Password = "weak",
            FirstName = "Taylor",
            LastName = "Runner",
            IdNumberOrPassport = "TEST-ID",
            Role = Roles.Participant
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(db.Users);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndToken()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponseDto>(ok.Value);
        Assert.NotEmpty(response.Token);
        Assert.Equal(user.Id, response.User.Id);
        var json = JsonSerializer.Serialize(ok.Value);
        Assert.DoesNotContain("PasswordHash", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Secret123!", json);
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller);
        var dto = new LoginDto { Email = "missing@raceday.co.za", Password = "Secret123!" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller);
        var dto = new LoginDto { Email = "runner@raceday.co.za", Password = "WrongPassword" };

        // Act
        var result = await controller.Login(dto);

        // Assert
        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password", unauthorized.Value);
        Assert.False(controller.HttpContext.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task ChangePassword_NoSession_ReturnsUnauthorized()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ChangePassword_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        using var db = TestDbFactory.Create();
        var user = ControllerTestHelper.AddParticipant(db, "runner@raceday.co.za", "Secret123!");
        var controller = CreateController(db);
        ControllerTestHelper.AttachUser(controller, user.Id, Roles.Participant);
        var dto = new ChangePasswordDto { CurrentPassword = "Secret123!", NewPassword = "NewSecret123!" };

        // Act
        var result = await controller.ChangePassword(dto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var login = await controller.Login(new LoginDto { Email = "runner@raceday.co.za", Password = "NewSecret123!" });
        Assert.IsType<OkObjectResult>(login);
    }

    private static AuthController CreateController(RaceDay.Api.Data.ApplicationDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-only-signing-key-at-least-32-bytes",
                ["Jwt:Issuer"] = "RaceDay.Api",
                ["Jwt:Audience"] = "RaceDay.Client"
            })
            .Build();
        return new AuthController(db, new JwtTokenService(config));
    }
}
