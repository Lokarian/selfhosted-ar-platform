using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Session;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.CreateArServerUser;

public record CreateArServerUserCommand : IRequest<(AppUser, string)>
{
    public CreateArServerUserCommand(Guid arSessionId)
    {
        ArSessionId = arSessionId;
    }

    public Guid ArSessionId { get; }
}

public class CreateArServerUserCommandHandler : IRequestHandler<CreateArServerUserCommand, (AppUser, string)>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public CreateArServerUserCommandHandler(IApplicationDbContext context, IIdentityService identityService,
        ITokenService tokenService)
    {
        _context = context;
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<(AppUser, string)> Handle(CreateArServerUserCommand request, CancellationToken cancellationToken)
    {
        string UserName = $"ARServer-{request.ArSessionId}-{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
        string Email = $"{UserName}@arserver.com";
        string Password = "ARServerPassword123!";

        AppUser appUser = new() { UserName = UserName, Email = Email, AccountType = AppUserAccountType.Service };
        _context.AppUsers.Add(appUser);
        (Result result, string userId) = await _identityService.CreateUserAsync(appUser, Password);
        if (!result.Succeeded)
        {
            _context.AppUsers.Remove(appUser);
            throw new BusinessRuleException(string.Join(";", result.Errors));
        }

        //add user to base session
        BaseSession? baseSession = await _context.BaseSessions.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.ArSessionId, cancellationToken);
        if (baseSession == null)
        {
            throw new NotFoundException(nameof(BaseSession), request.ArSessionId);
        }

        SessionMember sessionMember = new() { User = appUser, Session = baseSession };
        _context.SessionMembers.Add(sessionMember);

        await _context.SaveChangesAsync(cancellationToken);
        return (appUser, await _tokenService.CreateTokenAsync(appUser));
    }
}