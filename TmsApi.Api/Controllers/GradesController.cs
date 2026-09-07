using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Hubs;
using TmsApi.Api.Hubs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
namespace TmsApi.Api.Controllers;
[ApiController]
[Route("api/grades")]
[Authorize(Roles = "Instructor,Admin")]
public sealed class GradesController(
    TmsDbContext context,
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    public sealed record GradeRequest(int StudentId, int CourseId, decimal Score);
    public sealed record GradeResponse(string Id, bool Success);
    [HttpPost]
    public async Task<IActionResult> PostGrade([FromBody] GradeRequest request, CancellationToken ct)
    {
        if (request.Score < 0 || request.Score > 100)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid score",
                detail: "Score must be between 0 and 100.");
        }
        var course = await context.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Course not found",
                detail: $"Course with id '{request.CourseId}' was not found.");
        }
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (course.InstructorId != currentUserId)
            {
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Not your course",
                    detail: "You can't submit a grade because you're not instructor of this course.");
            }
        }
        var enrollment = await context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId, ct);
        if (enrollment is null || enrollment.Status != EnrollmentStatus.Approved)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Student not registered",
                detail: $"There is no student with id '{request.StudentId}' registered in this course.");
        }
        enrollment.Grade = request.Score;
        await context.SaveChangesAsync(ct);
        var courseCode = enrollment.Course?.Code ?? string.Empty;
        await hubContext.Clients.All.ReceiveGradePosted(courseCode, enrollment.StudentId, request.Score);
        return Created(string.Empty, new GradeResponse(enrollment.Id.ToString(), true));
    }
}