using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using CoreServer.Domain.Entities.AR;
using FluentValidation;

namespace CoreServer.Application.AR.Commands.CreateArSession;

public class CreateArSessionCommandValidator : SessionContextValidator<CreateArSessionCommand>
{
    public CreateArSessionCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService) :
        base(context, currentUserService)
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("BaseSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session")
            .MustAsync(NoExistingArSession).WithMessage("Ar session already exists");
    }

    private async Task<bool> NoExistingArSession(Guid sessionId, CancellationToken cancellationToken)
    {
        ArSession? ArSession = await _context.ArSessions.FindAsync(sessionId);
        return ArSession == null;
    }
}