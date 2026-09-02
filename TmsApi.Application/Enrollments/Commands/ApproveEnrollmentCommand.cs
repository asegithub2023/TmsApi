using MediatR;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

// InstructorId: when set, the enrollment's course must be taught by that
// instructor or the action is rejected as Forbidden. Pass null for Admin
// callers, who can act on any enrollment.
public record ApproveEnrollmentCommand(int Id, string? InstructorId)
    : IRequest<EnrollmentActionResult>;

public class ApproveEnrollmentHandler(IEnrollmentRepository repo)
    : IRequestHandler<ApproveEnrollmentCommand, EnrollmentActionResult>
{
    public async Task<EnrollmentActionResult> Handle(ApproveEnrollmentCommand command, CancellationToken ct)
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

        enrollment.Status = EnrollmentStatus.Approved;
        await repo.UpdateAsync(enrollment, ct);
        return EnrollmentActionResult.Success;
    }
}