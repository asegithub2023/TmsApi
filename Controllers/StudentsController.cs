using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("students")]
public class StudentsController : ControllerBase
{
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
}



