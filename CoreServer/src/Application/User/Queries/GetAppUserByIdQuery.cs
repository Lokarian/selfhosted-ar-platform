using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.User.Queries;

public class GetAppUserByIdQuery : IRequest<AppUser?>
{
    public Guid Id { get; set; }
}

public class GetUserQueryHandler : IRequestHandler<GetAppUserByIdQuery, AppUser?>
{
    private readonly IApplicationDbContext _context;

    public GetUserQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppUser?> Handle(GetAppUserByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.AppUsers.FindAsync(request.Id);
    }
}