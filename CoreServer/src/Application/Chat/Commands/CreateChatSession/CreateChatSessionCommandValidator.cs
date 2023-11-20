using CoreServer.Application.Chat.Commands.CreateChatSession;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;

namespace CoreServer.Application.Video.Commands.CreateVideoSession;

public class CreateChatSessionCommandValidator : SessionContextValidator<CreateChatSessionCommand>
{
    public CreateChatSessionCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService) :
        base(context, currentUserService)
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("BaseSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session")
            .MustAsync(NoExistingChatSession).WithMessage("Chat session already exists");
    }

    private async Task<bool> NoExistingChatSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var chatSession = await _context.ChatSessions.FindAsync(sessionId);
        return chatSession == null;
    }
}