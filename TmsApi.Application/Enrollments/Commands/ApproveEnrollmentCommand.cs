using MediatR;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public record ApproveEnrollmentCommand(int Id) : IRequest<bool>;

public class ApproveEnrollmentHandler(IEnrollmentRepository repo)
    : IRequestHandler<ApproveEnrollmentCommand, bool>
{
    public async Task<bool> Handle(ApproveEnrollmentCommand command, CancellationToken ct)
    {
        var enrollment = await repo.GetByIdAsync(command.Id, ct);
        if (enrollment is null)
        {
            return false;
        }

        enrollment.Status = EnrollmentStatus.Approved;
        await repo.UpdateAsync(enrollment, ct);
        return true;
    }
}