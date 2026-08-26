using MediatR;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public record RejectEnrollmentCommand(int Id) : IRequest<bool>;

public class RejectEnrollmentHandler(IEnrollmentRepository repo)
    : IRequestHandler<RejectEnrollmentCommand, bool>
{
    public async Task<bool> Handle(RejectEnrollmentCommand command, CancellationToken ct)
    {
        var enrollment = await repo.GetByIdAsync(command.Id, ct);
        if (enrollment is null)
        {
            return false;
        }

        enrollment.Status = EnrollmentStatus.Rejected;
        await repo.UpdateAsync(enrollment, ct);
        return true;
    }
}