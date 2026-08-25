using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using TmsApi.Application.DTOs;
//using TmsApi.Application.Services;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Interfaces;
namespace TmsApi.Api.Controllers;

[Authorize(Roles = "Instructor,Admin")]
[ApiController]
[Route("api/courses")]
[Tags("Courses")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class CoursesController(
    ICourseService courseService,
    IAuthorizationService authorizationService,
    LinkGenerator linkGenerator) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CourseDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get a course by ID")]
    [EndpointDescription("Returns course details with HATEOAS links. Returns 404 if the course does not exist.")]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        var selfPath = linkGenerator.GetPathByName(HttpContext, nameof(GetCourseById), new { id })
            ?? throw new InvalidOperationException("Unable to generate self link.");

        var enrollmentsPath = linkGenerator.GetPathByAction(
                HttpContext,
                action: "GetEnrollments",
                controller: "Enrollments",
                values: new { courseId = id })
            ?? throw new InvalidOperationException("Unable to generate enrollments link.");

        var links = new List<LinkDto>
        {
            new(selfPath, "self", "GET"),
            new(selfPath, "update", "PUT"),
            new(selfPath, "delete", "DELETE"),
            new(enrollmentsPath, "enrollments", "GET")
        };

        if (course.EnrollmentCount < course.MaxCapacity)
        {
            links.Add(new LinkDto(enrollmentsPath, "enroll", "POST"));
        }

        var detailDto = new CourseDetailDto
        {
            Id = course.Id,
            Code = course.Code,
            Title = course.Title,
            MaxCapacity = course.MaxCapacity,
            EnrollmentCount = course.EnrollmentCount,
            Links = links
        };

        return Ok(detailDto);
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<CourseResponseDto>), StatusCodes.Status200OK)]
    [EndpointSummary("List courses with pagination")]
    [EndpointDescription("Returns a paginated, optionally filtered list of TMS courses. PageSize is capped at 50.")]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await courseService.GetCoursesAsync(request, ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create a new course")]
    [EndpointDescription("Creates a course with a unique code. Returns 409 if the course code already exists.")]
    public async Task<IActionResult> CreateCourse(CreateCourseRequest request, CancellationToken ct)
    {
        if (await courseService.CodeExistsAsync(request.Code, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course code already exists",
                Detail = $"A course with code '{request.Code}' is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await courseService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetCourseById), new { id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Update a course")]
    [EndpointDescription("Updates a course. Only the assigned lead instructor or an Admin may edit. Returns 403 if the caller does not own the course.")]
    public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto dto, CancellationToken ct)
    {
        var course = await courseService.GetEntityByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
        if (!authResult.Succeeded)
        {
            return Forbid(); // 403 Forbidden when caller doesn't own the resource
        }

        await courseService.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Delete a course")]
    [EndpointDescription("Deletes a course when it has no active enrollments. Returns 409 if active student enrollments still exist.")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);
        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"Course with id '{id}' was not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var deleted = await courseService.DeleteAsync(id, ct);
        if (!deleted)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Course deletion blocked",
                Detail = "Cannot delete course: active student enrollments exist.",
                Status = StatusCodes.Status409Conflict
            });
        }

        return NoContent();
    }
}