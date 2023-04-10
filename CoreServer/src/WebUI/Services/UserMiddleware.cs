using System.Security.Claims;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Queries;
using MediatR;

namespace CoreServer.WebUI.Services;

public class UserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private IServiceScopeFactory _serviceScopeFactory;

    public UserMiddleware(
        RequestDelegate next,
        ILogger<UserMiddleware> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
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
            var Mediator = context.RequestServices.GetRequiredService<ISender>();
            currentUserService.User = await Mediator.Send(new GetAppUserByIdQuery { Id = Guid.Parse(userId) });
        }

        await this._next(context);
    }
}