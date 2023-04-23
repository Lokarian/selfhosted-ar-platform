using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Application.User.Commands.LoginUser;
using CoreServer.Application.User.Commands.RegisterUser;
using CoreServer.Application.User.Commands.UpdateAppUser;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebUI.Controllers;

public class UserController : ApiControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public UserController(ICurrentUserService currentUserService, IMapper mapper)
    {
        _currentUserService = currentUserService;
        _mapper = mapper;
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
    public async Task<ActionResult<AppUserDto>> Current()
    {
        AppUser? appUser = await Mediator.Send(new GetAppUserByIdQuery { Id = _currentUserService.User!.Id });
        return _mapper.Map<AppUserDto>(appUser);
    }

    [Authorize]
    [HttpPut]
    public async Task<ActionResult<AppUserDto>> Update(UpdateAppUserCommand command)
    {
        await Mediator.Send(command);
        AppUser? appUser = await Mediator.Send(new GetAppUserByIdQuery { Id = command.Id });
        return _mapper.Map<AppUserDto>(appUser);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<AppUserDto>> GetAppUserById(Guid id)
    {
        AppUser? appUser = await Mediator.Send(new GetAppUserByIdQuery { Id = id });
        return _mapper.Map<AppUserDto>(appUser);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PaginatedList<AppUserDto>>> GetAppUsersBy(
        [FromQuery] GetAppUsersByPartialNameWithPaginationQuery query)
    {
        return Ok(await Mediator.Send(query));
    }
}