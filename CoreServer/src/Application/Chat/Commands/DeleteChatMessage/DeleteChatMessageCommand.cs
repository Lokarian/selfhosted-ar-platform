﻿using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using MediatR;

namespace CoreServer.Application.Chat.Commands;

public class DeleteChatMessageCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteChatMessageCommandHandler : IRequestHandler<DeleteChatMessageCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteChatMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(DeleteChatMessageCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ChatMessages.FindAsync(request.Id);

        if (entity == null)
        {
            throw new NotFoundException(nameof(ChatMessage), request.Id);
        }

        _context.ChatMessages.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}