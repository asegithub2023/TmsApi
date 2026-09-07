namespace TmsApi.Application.DTOs;

/// <summary>Represents the public enrollment data returned by the API.</summary>
public record EnrollmentResponseDto(
    int Id,
    int CourseId,
    int StudentId,
    DateTime EnrolledAt);