using CoreServer.Application.Common.Interfaces;
using FluentValidation;

namespace CoreServer.Application.User.Commands.LoginUser;

public class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    private readonly IApplicationDbContext _context;

    public LoginUserCommandValidator(IApplicationDbContext context)
    {
        _context = context;
        RuleFor(v => v.UserName).NotEmpty().WithMessage("Username cannot be emtpy");
        RuleFor(v => v.Password).NotEmpty().WithMessage("Password cannot be emtpty");
        RuleFor(v => v.UserName).MustAsync(UserNameExists).WithMessage("User name does not exist");
    }

    private async Task<bool> UserNameExists(string userName, CancellationToken token)
    {
        return _context.AppUsers.Any(e => e.UserName == userName);
    }
}