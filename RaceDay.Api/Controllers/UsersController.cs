using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize]
    public IActionResult GetMe()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        var user = userId is null ? null : _db.Users.Find(userId.Value);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(UserMapper.ToResponse(user));
    }

    [HttpPut("me")]
    [Authorize]
    public IActionResult UpdateMe(UpdateProfileDto dto)
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        var user = userId is null ? null : _db.Users.Find(userId.Value);
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
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var memberships = _db.ClubMemberships.Where(m => m.ParticipantId == userId)
            .Select(membership => new MembershipResponseDto
            {
                Id = membership.Id,
                ClubId = membership.ClubId,
                DisciplineId = membership.DisciplineId,
                MembershipNumber = membership.MembershipNumber,
                JoinedAt = membership.JoinedAt,
                Status = membership.Status
            }).ToList();
        return Ok(memberships);
    }
}
