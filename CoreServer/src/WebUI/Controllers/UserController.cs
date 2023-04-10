using CoreServer.Application.User.Commands.LoginUser;
using CoreServer.Application.User.Queries;
using Microsoft.AspNetCore.Mvc;


namespace CoreServer.WebUI.Controllers;
public class UserController: ApiControllerBase
{
    //login Endpoint
    [HttpPost("loginTest")]
    public async Task<ActionResult<string>> Login(LoginUserCommand command)
    {
        return await Mediator.Send(command);
    }
}