using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Security;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.SendMessageToChat;

[Authorize]
public class SendMessageToChatCommand : IRequest<ChatMessage>
{
    public Guid SessionId { get; set; }
    public string Text { get; set; } = null!;
}

public class SendMessageToChatCommandHandler : IRequestHandler<SendMessageToChatCommand, ChatMessage>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SendMessageToChatCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ChatMessage> Handle(SendMessageToChatCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.BaseSessionId == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(ChatSession), request.SessionId);
        }


        var message = new ChatMessage { Text = request.Text, Sender = _currentUserService.User!, Session = session };

        _context.ChatMessages.Add(message);
        session.AddDomainEvent(new ChatMassageCreatedEvent(message));

        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }
}