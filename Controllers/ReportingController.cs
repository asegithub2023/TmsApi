using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/reporting")]
public class ReportingController : ControllerBase
{
    private readonly TmsDbContext _context;

    public ReportingController(TmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("active-honors")]
    public async Task<IActionResult> GetActiveHonorStudents()
    {
        var count = await _context.Students
            .Where(s => s.IsActive && s.GPA >= 3.0m)
            .CountAsync();
        return Ok(new { Count = count });
    }

    [HttpGet("courses/by-enrollment")]
    public async Task<IActionResult> GetCoursesByEnrollmentCount()
    {
        var list = await _context.Courses
            .Select(c => new
            {
                c.Title,
                EnrollmentCount = c.Enrollments.Count
            })
            .OrderByDescending(x => x.EnrollmentCount)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("enrollments/average-gpa-by-course")]
    public async Task<IActionResult> GetAverageGPAByCourse()
    {
        var list = await _context.Enrollments
            .GroupBy(e => e.Course.Title)
            .Select(g => new
            {
                Course = g.Key,
                AverageGPA = g.Average(e => e.Student.GPA)
            })
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("students/unenrolled")]
    public async Task<IActionResult> GetUnenrolledStudents()
    {
        var list = await _context.Students
            .Where(s => !s.Enrollments.Any())
            .Select(s => s.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("students/not-enrolled-in-any")]
    public async Task<IActionResult> GetStudentsNotEnrolledInAny()
    {
        var list = await _context.Students
            .LeftJoin(_context.Enrollments,
                s => s.Id,
                e => e.StudentId,
                (s, e) => new { s, e })
            .Where(x => x.e == null)
            .Select(x => x.s.Name)
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("students/n-plus-one-demo")]
    public async Task<IActionResult> GetStudentsNPlusOneDemo(CancellationToken cancellationToken)
    {
        var results = new List<object>();
        var students = await _context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var student in students)
        {
            var count = await _context.Enrollments
                .AsNoTracking()
                .CountAsync(e => e.StudentId == student.Id, cancellationToken);

            results.Add(new { student.Name, EnrollmentCount = count });
        }

        return Ok(results);
    }

    [HttpGet("students/n-plus-one-fixed")]
    public async Task<IActionResult> GetStudentsNPlusOneFixed(CancellationToken cancellationToken)
    {
        var report = await _context.Students
            .AsNoTracking()
            .Select(s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(report);
    }

    [HttpGet("students/soft-delete-admin")]
    public async Task<IActionResult> GetStudentsIncludingDeleted(CancellationToken cancellationToken)
    {
        var students = await _context.Students
            .IgnoreQueryFilters()
            .Select(s => new
            {
                s.Name,
                s.IsDeleted,
                s.IsActive
            })
            .ToListAsync(cancellationToken);

        return Ok(students);
    }

    [HttpPost("enrollments/archive-old")]
    public async Task<IActionResult> ArchiveOldEnrollments(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-6);
        var affected = await _context.Enrollments
            .Where(e => e.EnrolledAt < cutoff)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsArchived, true), cancellationToken);

        return Ok(new { UpdatedRows = affected });
    }
}


