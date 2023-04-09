using CoreServer.Application.User.Commands.LoginUser;
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
    //test Endpoint
    [HttpGet("test")]
    public async Task<ActionResult<string>> Test()
    {
        //return endpoint of request
        return Request.Path.ToString();
    }
}