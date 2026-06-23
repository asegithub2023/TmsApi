using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

[ApiController]
[Route("courses")]
public class CoursesController : ControllerBase
{
    private readonly TmsDbContext _context;

    public CoursesController(TmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public IActionResult GetCourseById(string id)
    {
        return Ok(new
        {
            Id = id,
            Title = "ASP.NET Core",
            Credits = 3
        });
    }

    [HttpGet("all")]
    public IActionResult GetAllCourses()
    {
        return Ok(new[]
        {
            new { Id = "CS101", Title = "Programming Fundamentals" },
            new { Id = "CS102", Title = "ASP.NET Core" }
        });
    }

     //Top 5 courses by enrollment count using GroupBy
    [HttpGet("top-5-by-enrollment")]
    public async Task<IActionResult> GetTop5CoursesByEnrollment(CancellationToken ct = default)
    {
        var topCourses = await _context.Enrollments
            .GroupBy(e => new { e.CourseId, e.Course.Title })  // Group by CourseId and Title
            .Select(g => new
            {
                CourseId = g.Key.CourseId,
                Title = g.Key.Title,
                StudentCount = g.Count()
            })
            .OrderByDescending(x => x.StudentCount)  // Order by count descending
            .Take(5)  // Top 5 only
            .ToListAsync(ct);

        return Ok(topCourses);
    }
}