using CoreServer.Application.Common.Interfaces;
using FluentValidation;

namespace CoreServer.Application.User.Commands.RegisterUser;

public class UpdateAppUserCommandValidator : AbstractValidator<UpdateAppUserCommand>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public UpdateAppUserCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext context,
        IIdentityService identityService)
    {
        _currentUserService = currentUserService;
        _context = context;
        _identityService = identityService;
        RuleFor(v => v.Id).NotEmpty();
        RuleFor(v => v.UserName).NotEmpty().MaximumLength(256);
        RuleFor(v => v.Email).NotEmpty().MaximumLength(256).EmailAddress();
        RuleFor(v => v).MustAsync(IsAdminOrOwnUser).WithMessage("You are not authorized to update this user");
    }

    private async Task<bool> IsAdminOrOwnUser(UpdateAppUserCommand command,
        CancellationToken cancellationToken)
    {
        if (await _identityService.IsInRoleAsync(_currentUserService.User!.Id, "Administrator"))
        {
            return true;
        }

        if (command.Id == _currentUserService.User!.Id)
        {
            return true;
        }

        return false;
    }
}