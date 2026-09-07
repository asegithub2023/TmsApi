using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;
using TmsApi.Application.Interfaces;
namespace TmsApi.Api.Controllers.V2;
[ApiController]
[Route("api/v{version:apiVersion}/certificates")]
[ApiVersion("2.0")]
[Authorize(Roles = "Student,Instructor,Admin")]
public sealed class CertificatesController(ICertificateService certificates) : ControllerBase
{
    public sealed record IssueRequest(int StudentId, string CourseCode);
    [HttpPost]
    public async Task<IActionResult> Issue([FromBody] IssueRequest req, CancellationToken ct)
    {
        try
        {
            var result = await certificates.IssueCertificateAsync(req.StudentId, req.CourseCode, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Certificate request rejected",
                detail: ex.Message);
        }
        catch (BrokenCircuitException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Certificate service unavailable",
                detail: "The certificate service is temporarily unavailable. Please try again in about 15 seconds.");
        }
        catch (TimeoutRejectedException)
        {
            return Problem(
                statusCode: StatusCodes.Status504GatewayTimeout,
                title: "Certificate service timed out",
                detail: "The certificate service took too long to respond. Please try again.");
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Certificate service error",
                detail: "The certificate service is currently having issues. Please try again.");
        }
    }
}