using System.Security.Claims;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using MediatR;

namespace WebUI.Services;

public class UserMiddleware
{
    private readonly ILogger _logger;
    private readonly RequestDelegate _next;
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
        string? userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            currentUserService.User = null;
        }
        else
        {
            ISender Mediator = context.RequestServices.GetRequiredService<ISender>();
            currentUserService.User = await Mediator.Send(new GetAppUserByIdQuery { Id = Guid.Parse(userId) });
        }

        await _next(context);
    }
}