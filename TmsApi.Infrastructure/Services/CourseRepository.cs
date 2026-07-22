using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Persistence;

public class CourseRepository : ICourseRepository
{
    private readonly TmsDbContext context;

    public CourseRepository(TmsDbContext context)
    {
        this.context = context;
    }

    public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
        context.Courses
            .Include(c => c.Enrollments)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, ct);
}