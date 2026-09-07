using MediatR;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
namespace TmsApi.Application.Enrollments.Commands;
public record RejectEnrollmentCommand(int Id, string? InstructorId)
    : IRequest<EnrollmentActionResult>;
public class RejectEnrollmentHandler(IEnrollmentRepository repo)
    : IRequestHandler<RejectEnrollmentCommand, EnrollmentActionResult>
{
    // The optional instructor ID scopes rejection to the instructor's own courses.
    public async Task<EnrollmentActionResult> Handle(RejectEnrollmentCommand command, CancellationToken ct)
    {
        var enrollment = await repo.GetByIdAsync(command.Id, ct);
        if (enrollment is null)
        {
            return EnrollmentActionResult.NotFound;
        }
        if (command.InstructorId is not null && enrollment.Course.InstructorId != command.InstructorId)
        {
            return EnrollmentActionResult.Forbidden;
        }
        enrollment.Status = EnrollmentStatus.Rejected;
        await repo.UpdateAsync(enrollment, ct);
        return EnrollmentActionResult.Success;
    }
}