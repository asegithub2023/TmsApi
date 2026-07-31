using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Utilities;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(ICachedCourseService cachedCourses) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] string? fields,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var courses = await cachedCourses.GetAllCoursesAsync(ct);
        var totalCount = courses.Count;

        var rows = courses
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CourseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.EnrollmentCount))
            .ToList();

        var shaped = rows.ShapeData(fields, CourseDtoFields.Allowed).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasNext = page < totalPages;
        var hasPrevious = page > 1;

        return Ok(new
        {
            data = shaped,
            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },
            links = new[]
            {
                new LinkDto(Url.Action(nameof(GetCourses), new { page, pageSize, fields })!, "self", "GET"),
                hasNext ? new LinkDto(Url.Action(nameof(GetCourses), new { page = page + 1, pageSize, fields })!, "next", "GET") : null,
                hasPrevious ? new LinkDto(Url.Action(nameof(GetCourses), new { page = page - 1, pageSize, fields })!, "prev", "GET") : null
            }.Where(l => l is not null).Select(l => l!).ToList()
        });
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetCourse(string code, CancellationToken ct)
    {
        var course = await cachedCourses.GetCourseAsync(code, ct);

        if (course is null)
            return NotFound();

        var dto = new CourseDto(course.Id, course.Code, course.Title, course.MaxCapacity, course.EnrollmentCount);

        return Ok(new
        {
            data = dto,
            links = new[]
            {
                new LinkDto(Url.Action(nameof(GetCourse), new { code })!, "self", "GET"),
                new LinkDto(Url.Action("Enroll", "Enrollments", new { courseCode = code })!, "enroll", "POST")
            }
        });
    }
}