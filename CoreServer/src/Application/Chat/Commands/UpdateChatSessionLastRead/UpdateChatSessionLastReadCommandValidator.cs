using CoreServer.Application.Chat.Commands.SendMessageToChat;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;

namespace CoreServer.Application.Chat.Commands;

public class UpdateChatSessionLastReadCommandValidator : SessionContextValidator<UpdateChatSessionLastReadCommand>
{
    public UpdateChatSessionLastReadCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService) : base(context, currentUserService)
    {
        RuleFor(x => x.SessionId)
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}