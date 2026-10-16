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
        return Ok(_db.Events
            .Where(ev => ev.Status == "Published")
            .Select(ev => new EventResponseDto
            {
                Id = ev.Id,
                OrganiserId = ev.OrganiserId,
                Title = ev.Title,
                Description = ev.Description,
                Date = ev.Date,
                DistanceKm = ev.DistanceKm,
                EventType = ev.EventType,
                VenueName = ev.VenueName,
                City = ev.City,
                Province = ev.Province,
                Status = ev.Status
            }).ToList());
    }

    [HttpGet("{eventId:int}")]
    public IActionResult GetEvent(int eventId)
    {
        var ev = _db.Events.FirstOrDefault(ev => ev.Id == eventId && ev.Status == "Published");
        if (ev is null)
        {
            return NotFound();
        }

        return Ok(ResponseMapper.ToResponse(ev));
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateEvent(CreateEventDto dto)
    {
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

        if (dto.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest("Event date cannot be in the past.");
        }

        var organiserId = CurrentUser.GetUserId(HttpContext);
        if (organiserId is null)
        {
            return Unauthorized();
        }

        if (!_db.Organisers.Any(o => o.UserId == organiserId))
        {
            return BadRequest("Organiser account is missing.");
        }

        var ev = new Event
        {
            OrganiserId = organiserId.Value,
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
        return Created($"/api/events/{ev.Id}", ResponseMapper.ToResponse(ev));
    }

    [HttpPut("{eventId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult UpdateEvent(int eventId, UpdateEventDto dto)
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

        if (dto.Date is not null && dto.Date < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return BadRequest("Event date cannot be in the past.");
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
        return Ok(ResponseMapper.ToResponse(ev));
    }

    [HttpDelete("{eventId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult DeleteEvent(int eventId)
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

        if (_db.Categories.Any(category => category.EventId == eventId))
        {
            return Conflict("Events with categories cannot be deleted.");
        }

        _db.Events.Remove(ev);
        _db.SaveChanges();
        return NoContent();
    }
}
