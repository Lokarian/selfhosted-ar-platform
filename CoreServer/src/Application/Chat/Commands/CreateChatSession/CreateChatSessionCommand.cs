using AutoMapper;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.CreateChatSession;

public class CreateChatSessionCommand : IRequest<ChatSessionDto>
{
    public Guid SessionId { get; set; }
}

public class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, ChatSessionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateChatSessionCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ChatSessionDto> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var baseSession = await _context.BaseSessions.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        var session = new ChatSession() { BaseSession = baseSession! };
        var members = baseSession!.Members.Select(x => new ChatMember() { Session = session, BaseMember = x });

        session.AddDomainEvent(new ChatSessionCreatedEvent(session));
        _context.ChatSessions.Add(session);
        _context.ChatMembers.AddRange(members);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ChatSessionDto>(session);
    }
}