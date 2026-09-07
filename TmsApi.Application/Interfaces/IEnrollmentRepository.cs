using TmsApi.Domain.Entities;
namespace TmsApi.Application.Interfaces;

/// <summary>Provides persistence operations for enrollment workflows.</summary>
public interface IEnrollmentRepository
{
    Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct);
    Task AddAsync(Enrollment enrollment, CancellationToken ct);
    Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct);
    Task<List<Enrollment>> GetAllAsync(CancellationToken ct);
    Task<Enrollment?> GetByIdAsync(int id, CancellationToken ct);
    Task UpdateAsync(Enrollment enrollment, CancellationToken ct);
}