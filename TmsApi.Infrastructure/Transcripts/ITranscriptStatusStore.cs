using TmsApi.Application.Transcripts;
namespace TmsApi.Infrastructure.Transcripts;
public interface ITranscriptStatusStore
{
    Task<TranscriptStatus> CreateAsync(string reportId, int studentId, CancellationToken ct);
    Task MarkProcessingAsync(string reportId, CancellationToken ct);
    Task MarkReadyAsync(string reportId, string downloadUrl, CancellationToken ct);
    Task MarkFailedAsync(string reportId, string error, CancellationToken ct);
    Task<TranscriptStatus?> GetAsync(string reportId, CancellationToken ct);
    Task<string?> GetReportIdForIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);
    Task LinkIdempotencyKeyAsync(string idempotencyKey, string reportId, CancellationToken ct);
    Task SaveContentAsync(string reportId, byte[] content, string contentType, string fileName, CancellationToken ct);
    Task<TranscriptContent?> GetContentAsync(string reportId, CancellationToken ct);
}
public record TranscriptContent(byte[] Bytes, string ContentType, string FileName);