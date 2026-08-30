namespace TmsApi.Application.DTOs;

public class UpdateCourseDto
{
    public string Title { get; set; } = string.Empty;

    // Only honored when the caller is Admin - the controller resets this back
    // to the course's existing InstructorId before calling the service when
    // the caller is an Instructor, so they can never reassign ownership.
    public string? InstructorId { get; set; }
}