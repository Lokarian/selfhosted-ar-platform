using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries.GetMyVideoSessions;

public class GetMyVideoSessionsQuery : IRequest<List<VideoSessionDto>>
{
    public bool ActiveOnly { get; set; } = false;
}

public class GetMyVideoSessionsQueryHandler : IRequestHandler<GetMyVideoSessionsQuery, List<VideoSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public GetMyVideoSessionsQueryHandler(IApplicationDbContext context, IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<List<VideoSessionDto>> Handle(GetMyVideoSessionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.VideoSessions
            .Include(x => x.Members)
            .ThenInclude(x => x.Streams)
            .Where(session => session.BaseSession.Members.Any(member => member.UserId == _currentUserService.User!.Id))
            .ProjectToListAsync<VideoSessionDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}