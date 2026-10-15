using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DisciplinesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public DisciplinesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult GetDisciplines()
    {
        return Ok(_db.Disciplines.ToList());
    }

    [HttpPost]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateDiscipline(CreateDisciplineDto dto)
    {
        var denied = SessionAuth.ForbidIfUnauthorized(HttpContext, Roles.Organiser);
        if (denied is not null)
        {
            return denied;
        }

        if (_db.Disciplines.Any(d => d.Name == dto.Name))
        {
            return Conflict("A discipline with this name already exists.");
        }

        var discipline = new Discipline
        {
            Name = dto.Name,
            Description = dto.Description
        };
        _db.Disciplines.Add(discipline);
        _db.SaveChanges();
        return Created($"/api/disciplines/{discipline.Id}", discipline);
    }
}
