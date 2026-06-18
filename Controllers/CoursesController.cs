using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("courses")]
public class CoursesController : ControllerBase
{
    [HttpGet("{id}")]
    public IActionResult GetCourseById(string id)
    {
        return Ok(new
        {
            Id = id,
            Title = "ASP.NET Core",
            Credits = 3
        });
    }

    [HttpGet("all")]
    public IActionResult GetAllCourses()
    {
        return Ok(new[]
        {
            new { Id = "CS101", Title = "Programming Fundamentals" },
            new { Id = "CS102", Title = "ASP.NET Core" }
        });
    }
}