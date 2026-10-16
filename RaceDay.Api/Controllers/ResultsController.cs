using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResultsController : ControllerBase
{
    private static readonly string[] ResultStatuses = ["Finished", "DNF", "DNS", "DQ"];

    private readonly ApplicationDbContext _db;

    public ResultsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("~/api/events/{eventId:int}/results")]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateResult(int eventId, CreateResultDto dto)
    {
        var ev = _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var registration = _db.Registrations.Include(r => r.Category)
            .FirstOrDefault(r => r.Id == dto.RegistrationId);
        if (registration is null || registration.Category?.EventId != eventId)
        {
            return NotFound();
        }

        if (registration.Status != "Active")
        {
            return Conflict("Results can only be recorded for active registrations.");
        }

        if (_db.Results.Any(r => r.RegistrationId == dto.RegistrationId))
        {
            return Conflict("A result already exists for this registration.");
        }

        if (!ResultStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid result status.");
        }

        if (dto.OverallPosition is <= 0 || dto.CategoryPosition is <= 0)
        {
            return BadRequest("Positions must be greater than zero.");
        }

        var validation = ValidateResultValues(dto.GunTime, dto.NetTime, dto.OverallPosition,
            dto.CategoryPosition, eventId, registration.CategoryId);
        if (validation is not null)
        {
            return validation;
        }

        var result = new Result
        {
            RegistrationId = dto.RegistrationId,
            GunTime = dto.GunTime,
            NetTime = dto.NetTime,
            OverallPosition = dto.OverallPosition,
            CategoryPosition = dto.CategoryPosition,
            Status = dto.Status
        };
        _db.Results.Add(result);
        _db.SaveChanges();
        return Created($"/api/results/{result.Id}", ResponseMapper.ToResponse(result));
    }

    [HttpPut("{resultId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult UpdateResult(int resultId, UpdateResultDto dto)
    {
        var result = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .FirstOrDefault(r => r.Id == resultId);
        if (result is null)
        {
            return NotFound();
        }

        var eventId = result.Registration?.Category?.EventId;
        var ev = eventId is null ? null : _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (dto.Status is not null && !ResultStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid result status.");
        }

        if (dto.OverallPosition is <= 0 || dto.CategoryPosition is <= 0)
        {
            return BadRequest("Positions must be greater than zero.");
        }

        var validation = ValidateResultValues(
            dto.GunTime ?? result.GunTime,
            dto.NetTime ?? result.NetTime,
            dto.OverallPosition ?? result.OverallPosition,
            dto.CategoryPosition ?? result.CategoryPosition,
            ev.Id,
            result.Registration!.CategoryId,
            result.Id);
        if (validation is not null)
        {
            return validation;
        }

        result.GunTime = dto.GunTime ?? result.GunTime;
        result.NetTime = dto.NetTime ?? result.NetTime;
        result.OverallPosition = dto.OverallPosition ?? result.OverallPosition;
        result.CategoryPosition = dto.CategoryPosition ?? result.CategoryPosition;
        result.Status = dto.Status ?? result.Status;
        _db.SaveChanges();
        return Ok(ResponseMapper.ToResponse(result));
    }

    [HttpGet("~/api/events/{eventId:int}/results")]
    public IActionResult GetForEvent(int eventId)
    {
        if (!_db.Events.Any(e => e.Id == eventId && e.Status == "Published"))
        {
            return NotFound();
        }

        var results = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .Where(r => r.Registration!.Category!.EventId == eventId &&
                r.Registration.Status == "Active")
            .Select(r => new ResultResponseDto
            {
                Id = r.Id,
                CategoryId = r.Registration!.CategoryId,
                GunTime = r.GunTime,
                NetTime = r.NetTime,
                OverallPosition = r.OverallPosition,
                CategoryPosition = r.CategoryPosition,
                Status = r.Status
            })
            .ToList();
        return Ok(results);
    }

    [HttpGet("me")]
    [RequireRole(Roles.Participant)]
    public IActionResult GetMine()
    {
        var userId = CurrentUser.GetUserId(HttpContext);
        if (userId is null)
        {
            return Unauthorized();
        }

        var results = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .Where(r => r.Registration!.ParticipantId == userId)
            .AsEnumerable()
            .Select(ResponseMapper.ToResponse)
            .ToList();
        return Ok(results);
    }

    [HttpGet("{resultId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    public IActionResult GetResult(int resultId)
    {
        var result = _db.Results
            .Include(r => r.Registration)
            .ThenInclude(r => r!.Category)
            .FirstOrDefault(r => r.Id == resultId);
        if (result is null)
        {
            return NotFound();
        }

        var userId = CurrentUser.GetUserId(HttpContext)!.Value;
        var role = CurrentUser.GetRole(HttpContext);
        if (role == Roles.Participant && result.Registration?.ParticipantId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (role == Roles.Organiser)
        {
            var eventId = result.Registration?.Category?.EventId;
            var ev = eventId is null ? null : _db.Events.Find(eventId);
            if (ev is null || ev.OrganiserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }
        }

        return Ok(ResponseMapper.ToResponse(result));
    }

    private IActionResult? ValidateResultValues(TimeOnly? gunTime, TimeOnly? netTime,
        int? overallPosition, int? categoryPosition, int eventId, int categoryId, int? resultId = null)
    {
        if (gunTime is not null && netTime is not null && netTime > gunTime)
        {
            return BadRequest("Net time cannot be greater than gun time.");
        }

        if (overallPosition is not null && _db.Results.Any(r =>
                r.Id != resultId &&
                r.OverallPosition == overallPosition &&
                r.Registration!.Category!.EventId == eventId))
        {
            return Conflict("Overall position is already assigned for this event.");
        }

        if (categoryPosition is not null && _db.Results.Any(r =>
                r.Id != resultId &&
                r.CategoryPosition == categoryPosition &&
                r.Registration!.CategoryId == categoryId))
        {
            return Conflict("Category position is already assigned.");
        }

        return null;
    }
}
