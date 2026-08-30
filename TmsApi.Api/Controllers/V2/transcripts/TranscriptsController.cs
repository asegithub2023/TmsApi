using System.Threading.Channels;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Transcripts;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/transcripts")]
[ApiVersion("2.0")]
[Authorize(Roles = "Student,Instructor,Admin")]
public class TranscriptsController(
    Channel<TranscriptRequest> channel,
    ITranscriptStatusStore statusStore) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript(
        [FromBody] TranscriptRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existing = await statusStore.GetReportIdForIdempotencyKeyAsync(idempotencyKey, ct);
            if (existing is not null)
            {
                var existingStatus = await statusStore.GetAsync(existing, ct);
                return Accepted(
                    Url.Action(nameof(GetStatus), new { id = existing }),
                    existingStatus);
            }
        }

        var reportId = Guid.NewGuid().ToString("N")[..12];
        var status = await statusStore.CreateAsync(reportId, request.StudentId, ct);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            await statusStore.LinkIdempotencyKeyAsync(idempotencyKey, reportId, ct);

        await channel.Writer.WriteAsync(request.WithReportId(reportId), ct);

        Response.Headers.RetryAfter = "5";
        return Accepted(
            Url.Action(nameof(GetStatus), new { id = reportId }),
            status);
    }

    [HttpGet("{id}/status")]
    public async Task<IActionResult> GetStatus(string id, CancellationToken ct)
    {
        var status = await statusStore.GetAsync(id, ct);
        return status is null
            ? NotFound(new ProblemDetails
            {
                Title = "Transcript not found",
                Detail = $"No transcript request with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            })
            : Ok(status);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> Download(string id, CancellationToken ct)
    {
        var status = await statusStore.GetAsync(id, ct);
        if (status is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transcript not found",
                Detail = $"No transcript request with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });
        }

        // A Student may only download their own transcript; Instructor/Admin
        // can download any (matching who's allowed to request one on behalf
        // of a student in the first place).
        if (User.IsInRole("Student") && !User.IsInRole("Instructor") && !User.IsInRole("Admin"))
        {
            var studentIdClaim = User.FindFirst("studentId")?.Value;
            if (!int.TryParse(studentIdClaim, out var studentId) || studentId != status.StudentId)
            {
                return Forbid();
            }
        }

        if (status.State != TranscriptState.Ready)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Transcript not ready",
                Detail = $"Report '{id}' is currently {status.State}.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var content = await statusStore.GetContentAsync(id, ct);
        if (content is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Transcript content missing",
                Detail = "The report is marked ready but its content could not be found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return File(content.Bytes, content.ContentType, content.FileName);
    }
}