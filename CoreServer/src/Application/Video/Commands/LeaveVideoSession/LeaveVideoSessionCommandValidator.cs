using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.LeaveVideoSession;

public class LeaveVideoSessionCommandValidator : AbstractValidator<LeaveVideoSessionCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _applicationDbContext;

    public LeaveVideoSessionCommandValidator(ICurrentUserService currentUserService,
        IApplicationDbContext applicationDbContext)
    {
        _currentUserService = currentUserService;
        _applicationDbContext = applicationDbContext;
        RuleFor(x => x.VideoMemberId)
            .MustAsync(MustBeOwnVideoMember).WithMessage("This is not your video member id");
    }

    private async Task<bool> MustBeOwnVideoMember(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await _applicationDbContext.VideoMembers
            .FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);
        if (member == null)
        {
            return false;
        }

        return member.BaseMember.UserId == _currentUserService.User!.Id;
    }
}