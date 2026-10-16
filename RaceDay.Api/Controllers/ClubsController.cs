using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClubsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ClubsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetClubs()
    {
        return Ok(_db.Clubs.Select(club => new ClubResponseDto
        {
            Id = club.Id,
            Name = club.Name,
            RegistrationNumber = club.RegistrationNumber,
            ContactEmail = club.ContactEmail,
            ContactNumber = club.ContactNumber,
            CreatedAt = club.CreatedAt
        }).ToList());
    }

    [HttpGet("{clubId:int}")]
    public IActionResult GetClub(int clubId)
    {
        var club = _db.Clubs.Find(clubId);
        if (club is null)
        {
            return NotFound();
        }

        return Ok(ResponseMapper.ToResponse(club));
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateClub(CreateClubDto dto)
    {
        if (_db.Clubs.Any(c => c.Name == dto.Name ||
            (dto.RegistrationNumber != null && c.RegistrationNumber == dto.RegistrationNumber)))
        {
            return Conflict("A club with this name or registration number already exists.");
        }

        var club = new Club
        {
            Name = dto.Name,
            RegistrationNumber = dto.RegistrationNumber,
            ContactEmail = dto.ContactEmail,
            ContactNumber = dto.ContactNumber,
            CreatedAt = DateTime.UtcNow
        };
        _db.Clubs.Add(club);
        _db.SaveChanges();
        return Created($"/api/clubs/{club.Id}", ResponseMapper.ToResponse(club));
    }

    [HttpPost("{clubId:int}/memberships")]
    [RequireRole(Roles.Participant)]
    public IActionResult JoinClub(int clubId, CreateMembershipDto dto)
    {
        if (!_db.Clubs.Any(c => c.Id == clubId) || !_db.Disciplines.Any(d => d.Id == dto.DisciplineId))
        {
            return NotFound();
        }

        var participantId = CurrentUser.GetUserId(HttpContext)!.Value;
        if (_db.ClubMemberships.Any(m => m.ParticipantId == participantId && m.DisciplineId == dto.DisciplineId))
        {
            return Conflict("Already a member of a club for this discipline.");
        }

        var membership = new ClubMembership
        {
            ParticipantId = participantId,
            ClubId = clubId,
            DisciplineId = dto.DisciplineId,
            MembershipNumber = dto.MembershipNumber,
            JoinedAt = dto.JoinedAt == default ? DateOnly.FromDateTime(DateTime.UtcNow) : dto.JoinedAt,
            Status = "Active"
        };
        _db.ClubMemberships.Add(membership);
        _db.SaveChanges();
        return Created("/api/users/me/memberships", ResponseMapper.ToResponse(membership));
    }
}
