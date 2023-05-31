using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.User.Commands.LoginUser;

public record LoginUserCommand : IRequest<String>
{
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, String>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginUserCommandHandler(IIdentityService identityService, ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<String> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        (Result result, AppUser? user) = await _identityService.LoginAsync(request.UserName, request.Password);
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First());
        }

        if (user.AccountType != AppUserAccountType.User)
        {
            throw new UnauthorizedAccessException("Only users can login");
        }

        return await _tokenService.CreateTokenAsync(user!);
    }
}