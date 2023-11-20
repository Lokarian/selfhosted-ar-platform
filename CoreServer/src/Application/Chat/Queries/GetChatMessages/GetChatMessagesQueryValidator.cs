using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries.GetChatMessages;

public class GetChatMessagesQueryValidator : SessionContextValidator<GetChatMessagesQuery>
{
    public GetChatMessagesQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext context) :
        base(context, currentUserService)
    {
        RuleFor(v => v.Count)
            .InclusiveBetween(0, 100).WithMessage("Count must be between 0 and 100.");
        RuleFor(v => v.SessionId)
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}