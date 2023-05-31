using AutoMapper;
using AutoMapper.QueryableExtensions;
using CoreServer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Queries.GetMyArSessions;

public class GetMyArSessionsQuery : IRequest<IEnumerable<ArSessionDto>>
{
}

public class GetMyArSessionsQueryHandler : IRequestHandler<GetMyArSessionsQuery, IEnumerable<ArSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyArSessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ArSessionDto>> Handle(GetMyArSessionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.User!.Id;
        return await _context.ArSessions
            .Include(s => s.Members)
            .ThenInclude(m => m.BaseMember)
            .Where(x => x.Members.Any(m => m.BaseMember.UserId == userId))
            .ProjectTo<ArSessionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}