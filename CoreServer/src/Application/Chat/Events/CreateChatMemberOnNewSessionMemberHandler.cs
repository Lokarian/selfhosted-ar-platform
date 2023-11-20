using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Events;

public class CreateChatMemberOnNewSessionMemberHandler : INotificationHandler<SessionMemberUpdatedEvent>
{
    private readonly IApplicationDbContext _context;

    public CreateChatMemberOnNewSessionMemberHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(SessionMemberUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var chatSession = await
            _context.ChatSessions.FirstOrDefaultAsync(x => x.BaseSessionId == notification.SessionMember.SessionId,
                cancellationToken);
        if (chatSession != null)
        {
            var chatMember = new ChatMember() { BaseMemberId = notification.SessionMember.Id,SessionId = chatSession.BaseSessionId};
            chatMember.AddDomainEvent(new ChatMemberUpdatedEvent(chatMember));
            await _context.ChatMembers.AddAsync(chatMember, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}