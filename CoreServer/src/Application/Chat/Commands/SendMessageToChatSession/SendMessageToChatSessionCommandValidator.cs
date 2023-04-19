using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.SendMessageToChatSession;

public class SendMessageToChatSessionCommandValidator : AbstractValidator<SendMessageToChatSessionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SendMessageToChatSessionCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Text).NotEmpty();
        RuleFor(x => x.SessionId).MustAsync(IsMemberOfSession).WithMessage("You are not a member of this session");
    }

    private async Task<bool> IsMemberOfSession(Guid sessionId, CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.User!.Id;
        ChatSession? session = await _context.ChatSessions
            .Include(s => s.Members)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        return session?.Members.Any(m => m.UserId == userId) ?? false;
    }
}