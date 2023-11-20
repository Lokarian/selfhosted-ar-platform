using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries.GetVideoSessionMembers;

public class GetVideoSessionMembersQueryValidator : SessionContextValidator<GetVideoSessionMembersQuery>
{
    public GetVideoSessionMembersQueryValidator(ICurrentUserService currentUserService, IApplicationDbContext context) :
        base(context, currentUserService)
    {
        RuleFor(x => x.VideoSessionId).NotEmpty().WithMessage("VideoSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}