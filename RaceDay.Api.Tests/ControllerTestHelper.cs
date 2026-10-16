using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public static class ControllerTestHelper
{
    // Gives the controller a fresh server-side session. When userId is supplied the
    // session already holds that user's ID and role, as if they had logged in.
    public static void AttachUser(ControllerBase controller, int? userId = null, string? role = null)
    {
        var context = new DefaultHttpContext { Session = new TestSession() };
        if (userId is not null)
        {
            CurrentUser.SignIn(context, userId.Value, role ?? string.Empty);
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
