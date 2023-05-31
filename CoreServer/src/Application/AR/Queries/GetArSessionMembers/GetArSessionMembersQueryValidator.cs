using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;

namespace CoreServer.Application.AR.Queries.GetArSessionMembers;

public class GetArSessionMembersQueryValidator : SessionContextValidator<GetArSessionMembersQuery>
{
    public GetArSessionMembersQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext context) :
        base(context, currentUserService)
    {
        RuleFor(x => x.ArSessionId).NotEmpty().WithMessage("ArSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}