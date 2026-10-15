using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AdminController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpPost("organisers")]
    public IActionResult CreateOrganiser(CreateOrganiserDto dto)
    {
        var expectedKey = _config["AdminProvisioningKey"];
        var providedKey = Request.Headers["X-Admin-Key"].ToString();
        if (string.IsNullOrWhiteSpace(expectedKey) || providedKey != expectedKey)
        {
            return Unauthorized();
        }

        if (_db.Users.Any(u => u.Email == dto.Email))
        {
            return BadRequest("Email is already registered.");
        }

        const string role = Roles.Organiser;
        if (!Roles.Allowed.Contains(role))
        {
            return BadRequest("Invalid role.");
        }

        var temporaryPassword = Guid.NewGuid().ToString("N")[..12];
        var user = new User
        {
            Email = dto.Email,
            PasswordHash = string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = role,
            IdNumberOrPassport = dto.IdNumberOrPassport,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);

        _db.Users.Add(user);
        _db.SaveChanges();
        _db.Organisers.Add(new Organiser
        {
            UserId = user.Id,
            ProvisionedBy = "RaceDay administrator",
            ProvisionedAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        return Created($"/api/users/me", new
        {
            user = UserMapper.ToResponse(user),
            temporaryPassword
        });
    }
}
