using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
[Authorize]
public class EnrollmentsController(
    IMediator mediator,
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    // Only Instructor/Admin manage the full enrollment queue across all students.
    [Authorize(Roles = "Instructor,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await mediator.Send(new GetAllEnrollmentsQuery(), ct);
        return Ok(list);
    }

    // Students self-enroll; Instructor/Admin can also enroll a student.
    [Authorize(Roles = "Student,Instructor,Admin")]
    [HttpPost]
    public async Task<IActionResult> Enroll(
        EnrollStudentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }

    // Approving/rejecting is an Instructor/Admin decision, not a student action.
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var approved = await mediator.Send(new ApproveEnrollmentCommand(id), ct);
        if (!approved)
        {
            return NotFound();
        }

        await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id.ToString(), "Approved");
        return NoContent();
    }

    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var rejected = await mediator.Send(new RejectEnrollmentCommand(id), ct);
        if (!rejected)
        {
            return NotFound();
        }

        await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id.ToString(), "Rejected");
        return NoContent();
    }

    // A student's own enrollments, scoped entirely from the studentId claim
    // embedded in their JWT at login - never a caller-supplied id, so there's
    // no way to view someone else's enrollments through this endpoint.
    [Authorize(Roles = "Student")]
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var studentIdClaim = User.FindFirst("studentId")?.Value;
        if (!int.TryParse(studentIdClaim, out var studentId))
        {
            return NotFound(new { detail = "Your account isn't linked to a student record." });
        }

        var list = await mediator.Send(new GetMyEnrollmentsQuery(studentId), ct);
        return Ok(list);
    }

    // NOTE: any authenticated user can currently view any studentId's schedule -
    // there's no ownership check tying the caller to `studentId` yet. Flagged as
    // a follow-up; out of scope for this authorization pass.
    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(
        int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}