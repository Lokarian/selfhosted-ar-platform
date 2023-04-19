using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using FluentValidation;

namespace CoreServer.Application.Chat.Commands;

public class DeleteChatMessageCommandValidator : AbstractValidator<DeleteChatMessageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteChatMessageCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
        RuleFor(v => v.Id).NotEmpty().WithMessage("Id cannot be empty");
        RuleFor(v => v.Id).MustAsync(MessageExists).WithMessage("Message does not exist");
        RuleFor(v => v.Id).MustAsync(MessageBelongsToUser).WithMessage("Message does not belong to user");
    }

    private async Task<bool> MessageExists(Guid id, CancellationToken token)
    {
        return _context.ChatMessages.Any(e => e.Id == id);
    }

    private async Task<bool> MessageBelongsToUser(Guid id, CancellationToken token)
    {
        ChatMessage? message = await _context.ChatMessages.FindAsync(id);
        return message.SenderId == _currentUserService.User!.Id;
    }
}