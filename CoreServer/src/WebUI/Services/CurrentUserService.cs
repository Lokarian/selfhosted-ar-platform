using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.WebUI.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISender _mediator;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, ISender mediator)
    {
        _httpContextAccessor = httpContextAccessor;
        _mediator = mediator;
    }

    public AppUser? User { get; set; }
}