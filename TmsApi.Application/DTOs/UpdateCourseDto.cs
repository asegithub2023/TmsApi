namespace TmsApi.Application.DTOs;
public class UpdateCourseDto
{
    public string Title { get; set; } = string.Empty;
    public string? InstructorId { get; set; }
}