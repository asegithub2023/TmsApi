using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Transcripts;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Transcripts;

namespace TmsApi.Infrastructure.Workers;

public class TranscriptWorker(
    Channel<TranscriptRequest> channel,
    IServiceScopeFactory scopeFactory,
    ITranscriptStatusStore statusStore,
    IHubContext<TmsHub, ITmsHubClient> hubContext,
    ILogger<TranscriptWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        logger.LogInformation("Transcript worker started.");

        await foreach (var request in channel.Reader.ReadAllAsync(ct))
        {
            var reportId = request.ReportId
                ?? throw new InvalidOperationException("ReportId must be set before queueing.");

            try
            {
                await statusStore.MarkProcessingAsync(reportId, ct);

                logger.LogInformation(
                    "Generating transcript {ReportId} for student {StudentId}",
                    reportId,
                    request.StudentId);

                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

                var student = await db.Students
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == request.StudentId, ct);

                var enrollments = await db.Enrollments
                    .AsNoTracking()
                    .Include(e => e.Course)
                    .Where(e => e.StudentId == request.StudentId)
                    .OrderBy(e => e.EnrolledAt)
                    .ToListAsync(ct);

                // Simulated processing delay - kept from the original stub so
                // the Queued -> Processing -> Ready pipeline stays observable.
                await Task.Delay(TimeSpan.FromSeconds(5), ct);

                var fileName = $"transcript-{reportId}.txt";
                var content = BuildTranscriptText(student, enrollments);
                await statusStore.SaveContentAsync(reportId, content, "text/plain", fileName, ct);

                var downloadUrl = $"/api/v2/transcripts/{reportId}/download";
                await statusStore.MarkReadyAsync(reportId, downloadUrl, ct);

                await hubContext.Clients
                    .Group(GroupNames.Student(request.StudentId.ToString()))
                    .ReceiveTranscriptReady(reportId, downloadUrl);

                logger.LogInformation(
                    "Transcript ready, notification sent: {ReportId} to {Group}",
                    reportId,
                    GroupNames.Student(request.StudentId.ToString()));
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.LogWarning("Worker shutdown: transcript {ReportId} did not complete", reportId);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate transcript {ReportId}", reportId);
                await statusStore.MarkFailedAsync(reportId, ex.Message, CancellationToken.None);
            }
        }
    }

    private static byte[] BuildTranscriptText(Student? student, List<Enrollment> enrollments)
    {
        var sb = new StringBuilder();
        sb.AppendLine("UNOFFICIAL TRANSCRIPT (TMS demo environment)");
        sb.AppendLine("======================================================");
        sb.AppendLine();

        if (student is null)
        {
            sb.AppendLine("Student record not found.");
        }
        else
        {
            sb.AppendLine($"Student:              {student.Name}");
            sb.AppendLine($"Registration Number:  {student.RegistrationNumber}");
            sb.AppendLine($"GPA:                  {student.GPA:0.00}");
        }

        sb.AppendLine();
        sb.AppendLine("Code       Title                                Grade   Status      Enrolled");
        sb.AppendLine("------------------------------------------------------------------------------");

        if (enrollments.Count == 0)
        {
            sb.AppendLine("(no enrollments on record)");
        }
        else
        {
            foreach (var e in enrollments)
            {
                var grade = e.Grade?.ToString("0.0") ?? "-";
                var title = Truncate(e.Course.Title, 36);
                sb.AppendLine(
                    $"{e.Course.Code,-11}{title,-37}{grade,-8}{e.Status,-12}{e.EnrolledAt:yyyy-MM-dd}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:u}");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "...";
}