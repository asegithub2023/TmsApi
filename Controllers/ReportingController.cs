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
}


