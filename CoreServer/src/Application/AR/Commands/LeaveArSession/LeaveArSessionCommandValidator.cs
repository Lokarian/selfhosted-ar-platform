using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.LeaveArSession;

public class LeaveArSessionCommandValidator : AbstractValidator<LeaveArSessionCommand>
{
    private readonly IApplicationDbContext _applicationDbContext;
    private readonly ICurrentUserService _currentUserService;

    public LeaveArSessionCommandValidator(ICurrentUserService currentUserService,
        IApplicationDbContext applicationDbContext)
    {
        _currentUserService = currentUserService;
        _applicationDbContext = applicationDbContext;
        RuleFor(x => x.ArMemberId)
            .MustAsync(MustBeOwnArMember).WithMessage("This is not your ar member id");
    }

    private async Task<bool> MustBeOwnArMember(Guid memberId, CancellationToken cancellationToken)
    {
        ArMember? member = await _applicationDbContext.ArMembers
            .FirstOrDefaultAsync(x => x.Id == memberId, cancellationToken);
        if (member == null)
        {
            return false;
        }

        return member.BaseMember.UserId == _currentUserService.User!.Id;
    }
}