using MediatR;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
namespace TmsApi.Application.Enrollments.Commands;
public record ApproveEnrollmentCommand(int Id, string? InstructorId)
    : IRequest<EnrollmentActionResult>;
public class ApproveEnrollmentHandler(IEnrollmentRepository repo)
    : IRequestHandler<ApproveEnrollmentCommand, EnrollmentActionResult>
{
    // The optional instructor ID scopes approval to the instructor's own courses.
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