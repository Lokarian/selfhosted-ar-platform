using CoreServer.Application.AR.Commands.JoinArSession;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using CoreServer.Domain.Entities.AR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.StartArServer;

public class StartArServerCommandValidator:SessionContextValidator<StartArServerCommand>
{
    public StartArServerCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService) : base(
        context, currentUserService)
    {
        RuleFor(x => x.ArSessionId).NotEmpty().WithMessage("ArSession Id must not be empty")
            .MustAsync(MustBeCurrentlyOffline).WithMessage("Server is currently running or starting")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
    
    public async Task<bool> MustBeCurrentlyOffline(Guid arSessionId, CancellationToken cancellationToken)
    {
        var arSession = await _context.ArSessions
            .FirstOrDefaultAsync(x => x.BaseSessionId == arSessionId, cancellationToken);
        return arSession.ServerState == ArServerState.Stopped;
    }
}