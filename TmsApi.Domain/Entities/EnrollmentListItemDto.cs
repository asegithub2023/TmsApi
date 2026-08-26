namespace TmsApi.Application.DTOs;

public record EnrollmentListItemDto(
    int Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string Status,
    DateTime EnrolledAt);