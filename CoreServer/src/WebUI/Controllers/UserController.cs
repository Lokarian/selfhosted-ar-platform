using CoreServer.Application.User.Commands.LoginUser;
using CoreServer.Application.User.Commands.RegisterUser;
using Microsoft.AspNetCore.Mvc;


namespace CoreServer.WebUI.Controllers;

public class UserController : ApiControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(LoginUserCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost("register")]
    public async Task<ActionResult<string>> Register(RegisterUserCommand command)
    {
        return await Mediator.Send(command);
    }
}