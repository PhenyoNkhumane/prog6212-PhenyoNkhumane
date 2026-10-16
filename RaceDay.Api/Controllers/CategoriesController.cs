using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("~/api/events/{eventId:int}/categories")]
    public IActionResult GetForEvent(int eventId)
    {
        if (!_db.Events.Any(e => e.Id == eventId && e.Status == "Published"))
        {
            return NotFound();
        }

        return Ok(_db.Categories.Where(c => c.EventId == eventId)
            .Select(category => new CategoryResponseDto
            {
                Id = category.Id,
                EventId = category.EventId,
                DisciplineId = category.DisciplineId,
                Name = category.Name,
                CategoryType = category.CategoryType,
                DistanceKm = category.DistanceKm,
                MinAge = category.MinAge,
                MaxAge = category.MaxAge,
                EntryFeeZar = category.EntryFeeZar,
                MaxSlots = category.MaxSlots,
                StartTime = category.StartTime
            }).ToList());
    }

    [HttpGet("{categoryId:int}")]
    public IActionResult GetCategory(int categoryId)
    {
        var category = _db.Categories.FirstOrDefault(c => c.Id == categoryId && c.Event!.Status == "Published");
        if (category is null)
        {
            return NotFound();
        }

        return Ok(ResponseMapper.ToResponse(category));
    }

    [HttpPost("~/api/events/{eventId:int}/categories")]
    [RequireRole(Roles.Organiser)]
    public IActionResult CreateCategory(int eventId, CreateCategoryDto dto)
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

        var typeError = ValidateCategoryBounds(dto.CategoryType, dto.DistanceKm, dto.MinAge, dto.MaxAge);
        if (typeError is not null)
        {
            return typeError;
        }

        if (dto.EntryFeeZar < 0 || dto.MaxSlots <= 0)
        {
            return BadRequest("Entry fee must be zero or more and max slots must be greater than zero.");
        }

        if (!_db.Disciplines.Any(d => d.Id == dto.DisciplineId))
        {
            return NotFound();
        }

        var category = new Category
        {
            EventId = eventId,
            DisciplineId = dto.DisciplineId,
            Name = dto.Name,
            CategoryType = dto.CategoryType,
            DistanceKm = dto.DistanceKm,
            MinAge = dto.MinAge,
            MaxAge = dto.MaxAge,
            EntryFeeZar = dto.EntryFeeZar,
            MaxSlots = dto.MaxSlots,
            StartTime = dto.StartTime
        };
        _db.Categories.Add(category);
        _db.SaveChanges();
        return Created($"/api/categories/{category.Id}", ResponseMapper.ToResponse(category));
    }

    [HttpPut("{categoryId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult UpdateCategory(int categoryId, UpdateCategoryDto dto)
    {
        var category = _db.Categories.Find(categoryId);
        if (category is null)
        {
            return NotFound();
        }

        var ev = _db.Events.Find(category.EventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        var categoryType = dto.CategoryType ?? category.CategoryType;
        var distance = dto.DistanceKm ?? category.DistanceKm;
        var minAge = dto.MinAge ?? category.MinAge;
        var maxAge = dto.MaxAge ?? category.MaxAge;
        var typeError = ValidateCategoryBounds(categoryType, distance, minAge, maxAge);
        if (typeError is not null)
        {
            return typeError;
        }

        category.Name = dto.Name ?? category.Name;
        category.CategoryType = categoryType;
        category.DistanceKm = distance;
        category.MinAge = minAge;
        category.MaxAge = maxAge;
        category.EntryFeeZar = dto.EntryFeeZar ?? category.EntryFeeZar;
        category.MaxSlots = dto.MaxSlots ?? category.MaxSlots;
        category.StartTime = dto.StartTime ?? category.StartTime;
        _db.SaveChanges();
        return Ok(ResponseMapper.ToResponse(category));
    }

    [HttpDelete("{categoryId:int}")]
    [RequireRole(Roles.Organiser)]
    public IActionResult DeleteCategory(int categoryId)
    {
        var category = _db.Categories.Find(categoryId);
        if (category is null)
        {
            return NotFound();
        }

        var ev = _db.Events.Find(category.EventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != CurrentUser.GetUserId(HttpContext))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (_db.Registrations.Any(registration => registration.CategoryId == categoryId))
        {
            return Conflict("Categories with registrations cannot be deleted.");
        }

        _db.Categories.Remove(category);
        _db.SaveChanges();
        return NoContent();
    }

    private IActionResult? ValidateCategoryBounds(string categoryType, decimal? distanceKm, int? minAge, int? maxAge)
    {
        if (categoryType == "Distance")
        {
            if (distanceKm is null or <= 0 || minAge is not null || maxAge is not null)
            {
                return BadRequest("Distance categories require a positive distance and no age bounds.");
            }
        }
        else if (categoryType == "Age")
        {
            if (distanceKm is not null || minAge is null || maxAge is null || minAge < 0 || maxAge < minAge)
            {
                return BadRequest("Age categories require valid inclusive age bounds and no distance.");
            }
        }
        else
        {
            return BadRequest("Invalid category type.");
        }

        return null;
    }
}
