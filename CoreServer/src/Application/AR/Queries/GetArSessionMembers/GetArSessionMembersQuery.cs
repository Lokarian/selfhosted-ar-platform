using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Video.Queries;
using MediatR;

namespace CoreServer.Application.AR.Queries.GetArSessionMembers;

public class GetArSessionMembersQuery : IRequest<List<VideoMemberDto>>
{
    public Guid ArSessionId { get; set; }
}

public class
    GetArSessionMembersQueryHandler : IRequestHandler<GetArSessionMembersQuery, List<VideoMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetArSessionMembersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<VideoMemberDto>> Handle(GetArSessionMembersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.VideoMembers.Where(x => x.SessionId == request.ArSessionId)
            .ProjectToListAsync<VideoMemberDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}