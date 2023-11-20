using AutoMapper;
using CoreServer.Application.Chat.Queries.GetChatMessages;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.EventHandlers;

public class ChatMessageCreatedEventHandler : INotificationHandler<ChatMassageCreatedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IUserProxy<IRpcChatService> _userProxy;
    private readonly IMapper _mapper;

    public ChatMessageCreatedEventHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IDateTime dateTime, IUserProxy<IRpcChatService> userProxy, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _userProxy = userProxy;
        _mapper = mapper;
    }

    public async Task Handle(ChatMassageCreatedEvent notification, CancellationToken cancellationToken)
    {
        ChatMessage message = notification.Message;
        ChatSession? session = await _context.ChatSessions.Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.BaseSessionId == message.SessionId, cancellationToken);
        if (session == null)
        {
            throw new NotFoundException(nameof(Session), message.SessionId);
        }

        var receiverIds = session.Members.Select(m => m.BaseMember.UserId);
        await (await _userProxy.Clients(receiverIds)).NewChatMessage(_mapper.Map<ChatMessageDto>(message));
    }
}