using FluentValidation;

namespace CoreServer.Application.Chat.Queries.GetChatMessages;

public class GetChatMessagesQueryValidator : AbstractValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator()
    {
        RuleFor(v => v.SessionId)
            .NotEmpty().WithMessage("SessionId is required.");
        RuleFor(v => v.Count)
            .InclusiveBetween(0, 100).WithMessage("Count must be between 1 and 100.");
    }
}