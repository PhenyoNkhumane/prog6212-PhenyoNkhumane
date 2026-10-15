using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private static readonly string[] EventTypes = ["Run", "Walk", "Cycle"];
    private static readonly string[] EventStatuses = ["Draft", "Published", "Cancelled"];

    private readonly ApplicationDbContext _db;

    public EventsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetEvents()
    {
        return Ok(_db.Events.ToList());
    }

    [HttpGet("{eventId:int}")]
    public IActionResult GetEvent(int eventId)
    {
        var ev = _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        return Ok(ev);
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateEvent(CreateEventDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        if (dto.DistanceKm <= 0)
        {
            return BadRequest("Distance must be greater than zero.");
        }

        if (!EventTypes.Contains(dto.EventType))
        {
            return BadRequest("Invalid event type.");
        }

        if (!EventStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid event status.");
        }

        var organiserId = SessionAuth.GetUserId(HttpContext)!.Value;
        if (!_db.Organisers.Any(o => o.UserId == organiserId))
        {
            return BadRequest("Organiser account is missing.");
        }

        var ev = new Event
        {
            OrganiserId = organiserId,
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            VenueName = dto.VenueName,
            City = dto.City,
            Province = dto.Province,
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            Status = dto.Status
        };
        _db.Events.Add(ev);
        _db.SaveChanges();
        return Created($"/api/events/{ev.Id}", ev);
    }

    [HttpPut("{eventId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult UpdateEvent(int eventId, UpdateEventDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        var ev = _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != SessionAuth.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (dto.DistanceKm is <= 0)
        {
            return BadRequest("Distance must be greater than zero.");
        }

        if (dto.EventType is not null && !EventTypes.Contains(dto.EventType))
        {
            return BadRequest("Invalid event type.");
        }

        if (dto.Status is not null && !EventStatuses.Contains(dto.Status))
        {
            return BadRequest("Invalid event status.");
        }

        ev.Title = dto.Title ?? ev.Title;
        ev.Description = dto.Description ?? ev.Description;
        ev.Date = dto.Date ?? ev.Date;
        ev.VenueName = dto.VenueName ?? ev.VenueName;
        ev.City = dto.City ?? ev.City;
        ev.Province = dto.Province ?? ev.Province;
        ev.DistanceKm = dto.DistanceKm ?? ev.DistanceKm;
        ev.EventType = dto.EventType ?? ev.EventType;
        ev.Status = dto.Status ?? ev.Status;
        _db.SaveChanges();
        return Ok(ev);
    }

    [HttpDelete("{eventId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult DeleteEvent(int eventId)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        var ev = _db.Events.Find(eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != SessionAuth.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        _db.Events.Remove(ev);
        _db.SaveChanges();
        return NoContent();
    }
}
