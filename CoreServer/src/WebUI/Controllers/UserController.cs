using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Commands;
using CoreServer.Application.User.Commands.LoginUser;
using CoreServer.Application.User.Commands.RegisterUser;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace CoreServer.WebUI.Controllers;

public class UserController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;

    public UserController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<ActionResult<string>> Login(LoginUserCommand command)
    {
        return await Mediator.Send(command);
    }

    [HttpPost]
    public async Task<ActionResult<string>> Register(RegisterUserCommand command)
    {
        return await Mediator.Send(command);
    }

    //current user
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<AppUser>> Current()
    {
        return await Mediator.Send(new GetAppUserByIdQuery { Id = _currentUserService.User!.Id });
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<AppUser>> Update(UpdateAppUserCommand command)
    {
        await Mediator.Send(command);
        return await Mediator.Send(new GetAppUserByIdQuery { Id = command.Id });
    }
}