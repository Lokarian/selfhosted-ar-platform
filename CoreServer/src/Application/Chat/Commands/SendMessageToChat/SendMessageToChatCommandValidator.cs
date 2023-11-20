using CoreServer.Application.Chat.Commands.SendMessageToChat;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;

namespace CoreServer.Application.Chat.Commands.SendMessageToChatSession;

public class SendMessageToChatCommandValidator : SessionContextValidator<SendMessageToChatCommand>
{
    public SendMessageToChatCommandValidator(IApplicationDbContext context,
        ICurrentUserService currentUserService) : base(context,currentUserService)
    {
        RuleFor(x => x.Text).NotEmpty().WithMessage("Message text must not be empty");
        RuleFor(x => x.SessionId).MustAsync(MustBeMemberOfSession).WithMessage("You are not a member of this session");
    }

}