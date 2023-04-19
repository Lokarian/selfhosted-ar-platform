using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
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
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public RegisterUserCommandHandler(IIdentityService identityService, ITokenService tokenService,
        IApplicationDbContext context)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _context = context;
    }

    public async Task<string> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        AppUser appUser = new() { UserName = request.UserName, Email = request.Email };
        _context.AppUsers.Add(appUser);
        (Result result, string user) = await _identityService.CreateUserAsync(appUser, request.Password);
        if (!result.Succeeded)
        {
            _context.AppUsers.Remove(appUser);
            throw new Exception(result.Errors.ToString());
        }

        return await _tokenService.CreateTokenAsync(appUser);
    }
}