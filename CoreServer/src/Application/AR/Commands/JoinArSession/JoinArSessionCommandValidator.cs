using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Validators;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.JoinArSession;

public class JoinArSessionCommandValidator : SessionContextValidator<JoinArSessionCommand>
{
    public JoinArSessionCommandValidator(IApplicationDbContext context, ICurrentUserService currentUserService) : base(
        context, currentUserService)
    {
        RuleFor(x => x.ArSessionId).NotEmpty().WithMessage("ArSession Id must not be empty")
            .MustAsync(MustBeMemberOfSession).WithMessage("User is not a member of this session");
    }
}