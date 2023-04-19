using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
//import System.Linq;
using System.Linq.Expressions;

namespace CoreServer.Application.Chat.Commands.CreateChatSession;

public class CreateChatSessionCommand : IRequest<ChatSession>
{
    public List<Guid> UserIds { get; set; } = null!;
    public string? Name { get; set; }
}

public class CreateChatSessionCommandHandler : IRequestHandler<CreateChatSessionCommand, ChatSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateChatSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ChatSession> Handle(CreateChatSessionCommand request, CancellationToken cancellationToken)
    {
        var users = await _context.AppUsers.AsTracking().Where(u => request.UserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);
        //add current user to chat session if not already in it
        if (users.All(u => u.Id != _currentUserService.User!.Id))
        {
            users.Add(_currentUserService.User!);
        }

        var entity = new ChatSession { Name = request.Name };
        var members = users.Select(u => new ChatMember(u, entity)).ToList();

        _context.ChatSessions.Add(entity);
        _context.ChatMembers.AddRange(members);
        entity.AddDomainEvent(new ChatSessionCreatedEvent(entity));
        await _context.SaveChangesAsync(cancellationToken);

        return entity;
    }
}