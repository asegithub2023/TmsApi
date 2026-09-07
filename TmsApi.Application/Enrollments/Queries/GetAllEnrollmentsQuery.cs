using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
namespace TmsApi.Application.Enrollments.Queries;
public record GetAllEnrollmentsQuery(string? InstructorId)
    : IRequest<IReadOnlyList<EnrollmentListItemDto>>;
public class GetAllEnrollmentsHandler(IEnrollmentRepository repo)
    : IRequestHandler<GetAllEnrollmentsQuery, IReadOnlyList<EnrollmentListItemDto>>
{
    public async Task<IReadOnlyList<EnrollmentListItemDto>> Handle(
        GetAllEnrollmentsQuery query, CancellationToken ct)
    {
        var enrollments = await repo.GetAllAsync(ct);
        if (query.InstructorId is not null)
        {
            enrollments = enrollments
                .Where(e => e.Course.InstructorId == query.InstructorId)
                .ToList();
        }
        return enrollments
            .Select(e => new EnrollmentListItemDto(
                e.Id,
                e.StudentId,
                e.Student.Name,
                e.CourseId,
                e.Course.Title,
                e.Status.ToString(),
                e.EnrolledAt))
            .ToList();
    }
}