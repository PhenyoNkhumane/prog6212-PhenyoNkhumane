using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegistrationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public RegistrationsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpPost("~/api/events/{eventId:int}/registrations")]
    [RequireRole(Roles.Participant)]
    public IActionResult Enrol(int eventId, EnrolDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant);
        if (denied is not null)
        {
            return denied;
        }

        var ev = _db.Events.Find(eventId);
        var category = _db.Categories.Find(dto.CategoryId);
        if (ev is null || category is null || category.EventId != eventId)
        {
            return NotFound();
        }

        var participantId = SessionAuth.GetUserId(HttpContext)!.Value;
        if (_db.Registrations.Any(r => r.ParticipantId == participantId && r.CategoryId == dto.CategoryId))
        {
            return Conflict("Already registered for this category.");
        }

        var activeCount = _db.Registrations.Count(r => r.CategoryId == dto.CategoryId && r.Status == "Active");
        if (activeCount >= category.MaxSlots)
        {
            return Conflict("Category is full.");
        }

        var registration = new Registration
        {
            ParticipantId = participantId,
            CategoryId = dto.CategoryId,
            RegistrationDate = DateTime.UtcNow,
            PaymentStatus = "Pending",
            AmountPaid = 0,
            RequiresTempAsaLicense = dto.RequiresTempAsaLicense,
            MedicalAidName = dto.MedicalAidName,
            MedicalAidNumber = dto.MedicalAidNumber,
            Status = "Active"
        };
        _db.Registrations.Add(registration);
        _db.SaveChanges();
        return Created($"/api/registrations/{registration.Id}", registration);
    }

    [HttpGet("me")]
    [RequireRole(Roles.Participant)]
    public IActionResult GetMine()
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant);
        if (denied is not null)
        {
            return denied;
        }

        var userId = SessionAuth.GetUserId(HttpContext)!.Value;
        return Ok(_db.Registrations.Where(r => r.ParticipantId == userId).ToList());
    }

    [HttpGet("~/api/events/{eventId:int}/registrations")]
    [RequireRole(Roles.Organiser)]
    public IActionResult GetForEvent(int eventId)
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

        var registrations = _db.Registrations
            .Include(r => r.Category)
            .Where(r => r.Category!.EventId == eventId)
            .ToList();
        return Ok(registrations);
    }

    [HttpGet("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    public IActionResult GetRegistration(int registrationId)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        return Ok(registration);
    }

    [HttpPut("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    public IActionResult UpdateRegistration(int registrationId, UpdateRegistrationDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var role = SessionAuth.GetRole(HttpContext);
        if (role == Roles.Participant)
        {
            registration.MedicalAidName = dto.MedicalAidName ?? registration.MedicalAidName;
            registration.MedicalAidNumber = dto.MedicalAidNumber ?? registration.MedicalAidNumber;
        }
        else
        {
            if (dto.PaymentStatus is not null &&
                dto.PaymentStatus is not "Pending" and not "Paid" and not "Refunded")
            {
                return BadRequest("Invalid payment status.");
            }

            if (dto.Status is not null && dto.Status is not "Active" and not "Cancelled")
            {
                return BadRequest("Invalid registration status.");
            }

            if (dto.AmountPaid is < 0)
            {
                return BadRequest("Amount paid cannot be negative.");
            }

            registration.PaymentStatus = dto.PaymentStatus ?? registration.PaymentStatus;
            registration.AmountPaid = dto.AmountPaid ?? registration.AmountPaid;
            registration.AssignedBibNumber = dto.AssignedBibNumber ?? registration.AssignedBibNumber;
            registration.Status = dto.Status ?? registration.Status;
        }

        _db.SaveChanges();
        return Ok(registration);
    }

    [HttpDelete("{registrationId:int}")]
    [RequireRole(Roles.Participant, Roles.Organiser)]
    public IActionResult CancelRegistration(int registrationId)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Participant, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        var registration = _db.Registrations.Include(r => r.Category).FirstOrDefault(r => r.Id == registrationId);
        if (registration is null)
        {
            return NotFound();
        }

        if (!CanAccess(registration))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        registration.Status = "Cancelled";
        _db.SaveChanges();
        return NoContent();
    }

    private bool CanAccess(Registration registration)
    {
        var userId = SessionAuth.GetUserId(HttpContext)!.Value;
        var role = SessionAuth.GetRole(HttpContext);
        if (role == Roles.Participant)
        {
            return registration.ParticipantId == userId;
        }

        var eventId = registration.Category?.EventId;
        var ev = eventId is null ? null : _db.Events.Find(eventId);
        return ev is not null && ev.OrganiserId == userId;
    }
}
