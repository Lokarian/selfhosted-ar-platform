using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands;

public class UpdateChatSessionLastReadCommand : IRequest
{
    public Guid SessionId { get; set; }
}

public class UpdateChatSessionLastReadCommandHandler : IRequestHandler<UpdateChatSessionLastReadCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMediator _mediator;

    public UpdateChatSessionLastReadCommandHandler(IApplicationDbContext context,
        ICurrentUserService currentUserService, IMediator mediator)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mediator = mediator;
    }

    public async Task Handle(UpdateChatSessionLastReadCommand request, CancellationToken cancellationToken)
    {
        ChatMember? chatMember =
            await _context.ChatMembers.FirstOrDefaultAsync(x =>
                x.BaseMember.UserId == _currentUserService.User!.Id && x.SessionId == request.SessionId,
                cancellationToken);
        if (chatMember == null)
        {
            throw new UnauthorizedAccessException("You are not a member of this chat session.");
        }

        chatMember.LastSeen = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _mediator.Publish(new ChatMemberUpdatedEvent(chatMember), cancellationToken);
        return;
    }
}