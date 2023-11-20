using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using MediatR;

namespace CoreServer.Application.Chat.Commands.DeleteChatMessage;

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

    public async Task Handle(DeleteChatMessageCommand request, CancellationToken cancellationToken)
    {
        ChatMessage? entity = await _context.ChatMessages.FindAsync(request.Id);

        if (entity == null)
        {
            throw new NotFoundException(nameof(ChatMessage), request.Id);
        }

        _context.ChatMessages.Remove(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return;
    }
}