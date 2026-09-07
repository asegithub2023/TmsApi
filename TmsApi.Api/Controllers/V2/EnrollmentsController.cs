using System.Security.Claims;
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
    [Authorize(Roles = "Instructor,Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var scopeInstructorId = GetScopeInstructorId();
        var list = await mediator.Send(new GetAllEnrollmentsQuery(scopeInstructorId), ct);
        return Ok(list);
    }
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
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var scopeInstructorId = GetScopeInstructorId();
        var result = await mediator.Send(new ApproveEnrollmentCommand(id, scopeInstructorId), ct);
        switch (result)
        {
            case EnrollmentActionResult.NotFound:
                return NotFound();
            case EnrollmentActionResult.Forbidden:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Not your course",
                    detail: "You can only approve enrollments for courses you are assigned to teach.");
        }
        await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id.ToString(), "Approved");
        return NoContent();
    }
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, CancellationToken ct)
    {
        var scopeInstructorId = GetScopeInstructorId();
        var result = await mediator.Send(new RejectEnrollmentCommand(id, scopeInstructorId), ct);
        switch (result)
        {
            case EnrollmentActionResult.NotFound:
                return NotFound();
            case EnrollmentActionResult.Forbidden:
                return Problem(
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Not your course",
                    detail: "You can only reject enrollments for courses you are assigned to teach.");
        }
        await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id.ToString(), "Rejected");
        return NoContent();
    }
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
    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(
        int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
    private string? GetScopeInstructorId()
    {
        if (User.IsInRole("Admin"))
        {
            return null;
        }
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}