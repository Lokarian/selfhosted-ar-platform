using CoreServer.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.CreateVideoStream;

public class CreateVideoStreamCommandValidator : AbstractValidator<CreateVideoStreamCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;

    public CreateVideoStreamCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext context)
    {
        _currentUserService = currentUserService;
        _context = context;
        RuleFor(v => v.VideoStreamMemberId)
            .MustAsync(MustBeOwnVideoMember).WithMessage("This is not your video member id");
    }

    private async Task<bool> MustBeOwnVideoMember(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await _context.VideoMembers.FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);
        if (member == null)
        {
            return false;
        }

        return member.BaseMember.UserId == _currentUserService.User!.Id;
    }
}