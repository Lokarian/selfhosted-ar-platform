using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using CoreServer.Domain.Entities.Chat;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class GetSessionMembersQueryValidator : SessionContextValidator<GetSessionMembersQuery>
{
    public GetSessionMembersQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext context) :
        base(context,currentUserService)
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("UserSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}