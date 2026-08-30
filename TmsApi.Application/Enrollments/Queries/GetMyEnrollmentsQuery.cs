using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public record GetMyEnrollmentsQuery(int StudentId) : IRequest<IReadOnlyList<EnrollmentListItemDto>>;

public class GetMyEnrollmentsHandler(IEnrollmentRepository repo)
    : IRequestHandler<GetMyEnrollmentsQuery, IReadOnlyList<EnrollmentListItemDto>>
{
    public async Task<IReadOnlyList<EnrollmentListItemDto>> Handle(
        GetMyEnrollmentsQuery query, CancellationToken ct)
    {
        var enrollments = await repo.GetByStudentIdAsync(query.StudentId, ct);

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