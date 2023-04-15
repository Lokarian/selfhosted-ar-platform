﻿using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Security;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.CreateChatSession;

[Authorize]
public class SendMessageToChatSessionCommand : IRequest<ChatMessage>
{
    public Guid SessionId { get; set; }
    public string Text { get; set; } = null!;
}

public class SendMessageToChatSessionCommandHandler : IRequestHandler<SendMessageToChatSessionCommand,ChatMessage>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SendMessageToChatSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ChatMessage> Handle(SendMessageToChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);
        
        if (session == null)
        {
            throw new NotFoundException(nameof(ChatSession), request.SessionId);
        }

        var message = new ChatMessage { Text = request.Text, Sender = _currentUserService.User!, Session = session };

        _context.ChatMessages.Add(message);
        session.AddDomainEvent(new MassageInChatCreatedEvent(message));

        await _context.SaveChangesAsync(cancellationToken);

        return message;
    }
}