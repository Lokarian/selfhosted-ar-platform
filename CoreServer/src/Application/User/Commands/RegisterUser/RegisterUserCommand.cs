using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.User.Commands.RegisterUser;

public class RegisterUserCommand : IRequest<string>
{
    public string Email { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
}

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, string>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IApplicationDbContext _context;

    public RegisterUserCommandHandler(IIdentityService identityService, ITokenService tokenService,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var appUser = new AppUser() { UserName = request.UserName, Email = request.Email };
        _context.AppUsers.Add(appUser);
        var (result, user) = await _identityService.CreateUserAsync(appUser, request.Password);
        if (!result.Succeeded)
        {
            _context.AppUsers.Remove(appUser);
            throw new Exception(result.Errors.ToString());
        }

        return await _tokenService.CreateTokenAsync(appUser);
    }
}