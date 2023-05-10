using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;

namespace CoreServer.Application.Video.Commands.CreateVideoSession;

public class CreateVideoSessionCommandValidator : SessionContextValidator<CreateVideoSessionCommand>
{
    public CreateVideoSessionCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService) : base(context, currentUserService)
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("BaseSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session").MustAsync(NoExistingVideoSession).WithMessage("Video session already exists");
    }
    
    private async Task<bool> NoExistingVideoSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var videoSession = await _context.VideoSessions.FindAsync(sessionId);
        return videoSession == null;
    }
}