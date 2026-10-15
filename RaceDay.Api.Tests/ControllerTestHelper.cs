using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public static class ControllerTestHelper
{
    public static void AttachSession(ControllerBase controller, int? userId = null, string? role = null)
    {
        var session = new TestSession();
        if (userId is not null)
        {
            session.SetInt32(SessionKeys.UserId, userId.Value);
            session.SetString(SessionKeys.Role, role ?? string.Empty);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = session }
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
            IdNumberOrPassport = "9001015009089"
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
            IdNumberOrPassport = "8001015009087"
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
