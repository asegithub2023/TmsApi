using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Hubs;
using TmsApi.Api.Hubs;
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
            return BadRequest(new { error = "Score must be between 0 and 100." });
        }

        var enrollment = await context.Enrollments
            .Include(e => e.Course)
            .FirstOrDefaultAsync(e => e.StudentId == request.StudentId && e.CourseId == request.CourseId, ct);

        if (enrollment is null)
        {
            return NotFound(new { error = "Enrollment not found." });
        }

        enrollment.Grade = request.Score;
        await context.SaveChangesAsync(ct);

        var courseCode = enrollment.Course?.Code ?? string.Empty;
        await hubContext.Clients.All.ReceiveGradePosted(courseCode, enrollment.StudentId, request.Score);

        return Created(string.Empty, new GradeResponse(enrollment.Id.ToString(), true));
    }
}