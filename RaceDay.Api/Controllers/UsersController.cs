using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public UsersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("me")]
    [RequireRole]
    public IActionResult GetMe()
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext);
        if (denied is not null)
        {
            return denied;
        }

        var user = _db.Users.Find(SessionAuth.GetUserId(HttpContext)!.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("me")]
    [RequireRole]
    public IActionResult UpdateMe(UpdateProfileDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext);
        if (denied is not null)
        {
            return denied;
        }

        var user = _db.Users.Find(SessionAuth.GetUserId(HttpContext)!.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        if (dto.Email is not null &&
            _db.Users.Any(u => u.Email == dto.Email && u.Id != user.Id))
        {
            return BadRequest("Email is already registered.");
        }

        user.FirstName = dto.FirstName ?? user.FirstName;
        user.LastName = dto.LastName ?? user.LastName;
        user.Email = dto.Email ?? user.Email;
        user.IdNumberOrPassport = dto.IdNumberOrPassport ?? user.IdNumberOrPassport;
        user.AsaPermanentLicenseNumber = dto.AsaPermanentLicenseNumber ?? user.AsaPermanentLicenseNumber;
        _db.SaveChanges();
        return Ok(UserMapper.ToResponse(user));
    }

    [HttpGet("me/memberships")]
    [RequireRole(Roles.Participant)]
    public IActionResult GetMyMemberships()
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant);
        if (denied is not null)
        {
            return denied;
        }

        var userId = SessionAuth.GetUserId(HttpContext)!.Value;
        var memberships = _db.ClubMemberships.Where(m => m.ParticipantId == userId).ToList();
        return Ok(memberships);
    }
}
