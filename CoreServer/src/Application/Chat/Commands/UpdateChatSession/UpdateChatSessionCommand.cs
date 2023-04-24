using AutoMapper;
using CoreServer.Application.Chat.Queries.GetMyChatSessions;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Commands.UpdateChatSession;

public record UpdateChatSessionCommand : IRequest<ChatSession>
{
    public Guid SessionId { get; init; }
    public List<Guid>? UserIds { get; init; }
    public string? Name { get; init; }
}

public class UpdateChatSessionCommandHandler : IRequestHandler<UpdateChatSessionCommand, ChatSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateChatSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ChatSession> Handle(UpdateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _context.ChatSessions
            .Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);

        if (session == null)
        {
            throw new NotFoundException(nameof(session), request.SessionId);
        }

        if (request.UserIds != null)
        {
            var users = await _context.AppUsers.Where(x => request.UserIds.Contains(x.Id))
                .ToListAsync(cancellationToken);
            session.Members.Clear();
            users.Select(x => new ChatMember() { UserId = x.Id, SessionId = session.Id }).ToList()
                .ForEach(x => session.Members.Add(x));
        }

        if (request.Name != null)
        {
            session.Name = request.Name;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }
}