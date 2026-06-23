using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

[ApiController]
[Route("students")]
public class StudentsController : ControllerBase
{
    private readonly TmsDbContext _context;

    public StudentsController(TmsDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public IActionResult GetStudentById(string id)
    {
        return Ok(new
        {
            Id = id,
            Name = "Abebe",
            Department = "Computer Science"
        });
    }

    [HttpGet("all")]
    public IActionResult GetAllStudents()
    {
        return Ok(new[]
        {
            new { Id = "S001", Name = "Abebe" },
            new { Id = "S002", Name = "Kebede" }
        });
    }

    // Pagination with OrderBy, Skip, Take
    [HttpGet("paged")]
    public async Task<IActionResult> GetPagedStudents(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        int page = pageNumber < 1 ? 1 : pageNumber;
        int size = pageSize < 1 ? 20 : pageSize;

        var students = await _context.Students
            .OrderBy(s => s.Name)  // Required: stable sort before Skip/Take
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        return Ok(new
        {
            PageNumber = page,
            PageSize = size,
            Count = students.Count,
            Students = students
        });
    }
}