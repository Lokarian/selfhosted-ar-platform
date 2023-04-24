using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries.GetChatMessages;

public class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetChatMessagesQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
        RuleFor(v => v.SessionId)
            .NotEmpty().WithMessage("SessionId is required.");
        RuleFor(v => v.Count)
            .InclusiveBetween(0, 100).WithMessage("Count must be between 1 and 100.");
        RuleFor(v => v.SessionId)
            .MustAsync(IsMember).WithMessage("You are not a participant of this chat session.");
    }

    private Task<bool> IsMember(Guid sessionId, CancellationToken cancellationToken)
    {
        return _context.ChatSessions
            .AnyAsync(s => s.Id == sessionId && s.Members.Any(p => p.UserId == _currentUserService.User.Id),
                cancellationToken);
    }
}