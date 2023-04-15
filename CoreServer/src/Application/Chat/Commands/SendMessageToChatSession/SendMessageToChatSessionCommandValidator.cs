using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.CreateChatSession;

public class SendMessageToChatSessionCommandValidator : AbstractValidator<SendMessageToChatSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SendMessageToChatSessionCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        this._context = context;
        this._currentUserService = currentUserService;
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.SessionId).MustAsync(IsMemberOfSession).WithMessage("You are not a member of this session");
    }

    private async Task<bool> IsMemberOfSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.User!.Id;
        var session = await _context.ChatSessions
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        return session?.Members.Any(m => m.UserId == userId) ?? false;
    }
}