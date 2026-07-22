using MediatR;
using TmsApi.Application.Interfaces;

namespace TmsApi.Application.Enrollments.Queries;

public record GetStudentScheduleQuery(int StudentId) : IRequest<ScheduleDto>;

public record ScheduleDto(int StudentId, IReadOnlyList<ScheduleItemDto> Items);

public record ScheduleItemDto(string CourseCode, string CourseTitle, string SessionInfo);

public class GetStudentScheduleHandler(IEnrollmentRepository repo)
    : IRequestHandler<GetStudentScheduleQuery, ScheduleDto>
{
    public async Task<ScheduleDto> Handle(
        GetStudentScheduleQuery query, CancellationToken ct)
    {
        var enrollments = await repo.GetByStudentIdAsync(query.StudentId, ct);

        var items = enrollments.Select(e => new ScheduleItemDto(
                e.Course.Code,
                e.Course.Title,
                "TBD"))
            .ToList();

        return new ScheduleDto(query.StudentId, items);
    }
}