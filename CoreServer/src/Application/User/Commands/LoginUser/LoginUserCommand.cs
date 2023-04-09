using CoreServer.Application.Common.Interfaces;
using MediatR;

namespace Microsoft.Extensions.DependencyInjection.User.Commands;

public record LoginUserCommand : IRequest<String>
{
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, String>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;

    public LoginUserCommandHandler(IIdentityService identityService,ITokenService tokenService)
    {
        _identityService = identityService;
        _tokenService = tokenService;
    }

    public async Task<String> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var (result,user)=await _identityService.LoginAsync(request.UserName, request.Password);
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.ToString());
        }
        return await _tokenService.CreateTokenAsync(user!);
    }
}