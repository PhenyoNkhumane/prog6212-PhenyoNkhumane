using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("register")]
    public IActionResult Register(RegisterDto dto)
    {
        if (_db.Users.Any(u => u.Email == dto.Email))
        {
            return BadRequest("Email is already registered.");
        }

        const string role = Roles.Participant;
        if (!Roles.Allowed.Contains(role))
        {
            return BadRequest("Invalid role.");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = role,
            IdNumberOrPassport = dto.IdNumberOrPassport,
            AsaPermanentLicenseNumber = dto.AsaPermanentLicenseNumber,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        _db.SaveChanges();
        _db.Participants.Add(new Participant { UserId = user.Id });
        _db.SaveChanges();

        return Created($"/api/users/me", UserMapper.ToResponse(user));
    }

    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized("Invalid email or password");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        SessionAuth.SignIn(HttpContext, user.Id, user.Role);
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        SessionAuth.SignOut(HttpContext);
        return Ok(new { message = "Signed out." });
    }

    [HttpPut("password")]
    [RequireRole]
    public IActionResult ChangePassword(ChangePasswordDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext);
        if (denied is not null)
        {
            return denied;
        }

        var userId = SessionAuth.GetUserId(HttpContext)!.Value;
        var user = _db.Users.Find(userId);
        if (user is null)
        {
            return Unauthorized();
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.MustChangePassword = false;
        _db.SaveChanges();
        return NoContent();
    }
}
