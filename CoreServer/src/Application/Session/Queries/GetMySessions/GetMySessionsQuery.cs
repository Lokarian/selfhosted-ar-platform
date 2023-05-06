using AutoMapper;
using AutoMapper.QueryableExtensions;
using CoreServer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Session.Queries.GetMySessions;

public class GetMySessionsQuery : IRequest<IEnumerable<SessionDto>>
{
}

public class GetMySessionsQueryHandler : IRequestHandler<GetMySessionsQuery, IEnumerable<SessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMySessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService, IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SessionDto>> Handle(GetMySessionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.User!.Id;
        return await _context.UserSessions
            .Where(x => x.Members.Any(m => m.UserId == userId&&m.DeletedAt==null))
            .ProjectTo<SessionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}