using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries.GetVideoSessionMembers;

public class GetVideoSessionMembersQuery : IRequest<List<VideoMemberDto>>
{
    public Guid VideoSessionId { get; set; }
}

public class
    GetVideoSessionMembersQueryHandler : IRequestHandler<GetVideoSessionMembersQuery, List<VideoMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVideoSessionMembersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<VideoMemberDto>> Handle(GetVideoSessionMembersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.VideoMembers.Where(x => x.SessionId == request.VideoSessionId)
            .ProjectToListAsync<VideoMemberDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}