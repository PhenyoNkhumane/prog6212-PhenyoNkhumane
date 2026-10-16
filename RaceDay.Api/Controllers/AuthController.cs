using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (!Roles.Allowed.Contains(dto.Role))
        {
            return BadRequest("Invalid role.");
        }

        if (!IsStrongPassword(dto.Password))
        {
            return BadRequest("Password must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters.");
        }

        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
        {
            return BadRequest("Email is already registered.");
        }

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Role = dto.Role,
            IdNumberOrPassport = dto.IdNumberOrPassport,
            AsaPermanentLicenseNumber = dto.AsaPermanentLicenseNumber,
            MustChangePassword = false,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        if (user.Role == Roles.Participant)
        {
            _db.Participants.Add(new Participant { UserId = user.Id });
        }
        else
        {
            _db.Organisers.Add(new Organiser
            {
                UserId = user.Id,
                ProvisionedBy = "Self-registration",
                ProvisionedAt = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        // Registering also logs the user in: store ID and role in the server-side session.
        CurrentUser.SignIn(HttpContext, user.Id, user.Role);
        return Created("/api/users/me", UserMapper.ToResponse(user));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user is null)
        {
            return Unauthorized("Invalid email or password");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        CurrentUser.SignIn(HttpContext, user.Id, user.Role);
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        CurrentUser.SignOut(HttpContext);
        return NoContent();
    }

    [HttpPut("password")]
    [RequireLogin]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!IsStrongPassword(dto.NewPassword))
        {
            return BadRequest("Password must be at least 12 characters and include upper-case, lower-case, numeric, and non-alphanumeric characters.");
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized("Invalid email or password");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static bool IsStrongPassword(string password) =>
        password.Length >= 12 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(c => !char.IsLetterOrDigit(c));
}
