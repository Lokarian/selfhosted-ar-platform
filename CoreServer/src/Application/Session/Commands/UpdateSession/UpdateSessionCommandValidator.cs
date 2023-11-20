using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Session.Commands.UpdateSession;

public class UpdateSessionCommandValidator : SessionContextValidator<UpdateSessionCommand>
{

    public UpdateSessionCommandValidator(ICurrentUserService currentUserService, IApplicationDbContext context):base(context,currentUserService)
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SessionId).MustAsync(MustBeMemberOfSession).WithMessage("You are not a member of this session");
    }
}