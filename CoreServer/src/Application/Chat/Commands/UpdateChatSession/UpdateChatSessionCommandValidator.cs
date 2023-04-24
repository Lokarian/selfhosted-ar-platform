using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.UpdateChatSession;

public class UpdateChatSessionCommandValidator : AbstractValidator<UpdateChatSessionCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public UpdateChatSessionCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SessionId).MustAsync(MustBeMemberOfSession).WithMessage("You are not a member of this session");
    }

    private Task<bool> MustBeMemberOfSession(Guid sessionId, CancellationToken arg3)
    {
        return _context.ChatSessions.AnyAsync(x =>
            x.Id == sessionId && x.Members.Any(y => y.UserId == _currentUserService.User.Id));
    }
}