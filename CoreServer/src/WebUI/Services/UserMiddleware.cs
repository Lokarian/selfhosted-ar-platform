using System.Security.Claims;
using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.User.Queries;
using CoreServer.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

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
            
            //also check if a specific connection is being used
            string? userConnectionId = context.Request.Headers["userconnectionid"];
            if (userConnectionId != null)
            {
                if(!Guid.TryParse(userConnectionId, out Guid userConnectionIdGuid))
                {
                    throw new NotFoundException(nameof(UserConnection));
                }
                IApplicationDbContext _context = context.RequestServices.GetRequiredService<IApplicationDbContext>();
                currentUserService.Connection = await _context.UserConnections
                    .Where(x => x.UserId == currentUserService.User!.Id && x.Id == userConnectionIdGuid&&x.DisconnectedAt==null)
                    .FirstOrDefaultAsync();
                if (currentUserService.Connection is null)
                {
                    throw new NotFoundException(nameof(UserConnection));
                }
            }
        }
        await _next(context);
    }
}