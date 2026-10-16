using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public static class ControllerTestHelper
{
    public static void AttachUser(ControllerBase controller, int? userId = null, string? role = null)
    {
        var context = new DefaultHttpContext();
        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()),
                new Claim(ClaimTypes.Role, role ?? string.Empty)
            ], "Test"));
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = context
        };
    }

    public static User AddParticipant(ApplicationDbContext db, string email, string password)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Email = email,
            PasswordHash = "pending",
            FirstName = "Test",
            LastName = "Participant",
            Role = Roles.Participant,
            IdNumberOrPassport = "TEST-PARTICIPANT-ID"
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
        db.Participants.Add(new Participant { UserId = user.Id });
        db.SaveChanges();
        return user;
    }

    public static User AddOrganiser(ApplicationDbContext db, string email, string password)
    {
        var hasher = new PasswordHasher<User>();
        var user = new User
        {
            Email = email,
            PasswordHash = "pending",
            FirstName = "Test",
            LastName = "Organiser",
            Role = Roles.Organiser,
            IdNumberOrPassport = "TEST-ORGANISER-ID"
        };
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        db.SaveChanges();
        db.Organisers.Add(new Organiser
        {
            UserId = user.Id,
            ProvisionedBy = "RaceDay administrator"
        });
        db.SaveChanges();
        return user;
    }
}
