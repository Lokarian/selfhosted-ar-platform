using System.Security.Claims;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Queries;
using MediatR;

namespace CoreServer.WebUI.Services;

public class UserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly ISender _mediator;

    public UserMiddleware(
        RequestDelegate next,
        ILogger<UserMiddleware> logger,
        ISender mediator)
    {
        _next = next;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Invoke(
        HttpContext context,
        ICurrentUserService currentUserService)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            currentUserService.User = null;
        }
        else
        {
            currentUserService.User = await _mediator.Send(new GetAppUserByIdQuery { Id = Guid.Parse(userId) });
        }
        await this._next(context);
    }
}