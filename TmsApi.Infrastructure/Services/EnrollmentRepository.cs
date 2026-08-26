using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Persistence;

public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly TmsDbContext context;

    public EnrollmentRepository(TmsDbContext context)
    {
        this.context = context;
    }

    public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }

    public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync(ct);

    public Task<List<Enrollment>> GetAllAsync(CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

    public Task<Enrollment?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Enrollments.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task UpdateAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Update(enrollment);
        await context.SaveChangesAsync(ct);
    }
}