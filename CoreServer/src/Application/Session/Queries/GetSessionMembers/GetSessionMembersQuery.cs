using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Session.Queries.GetMySessions;
using MediatR;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class GetSessionMembersQuery : IRequest<IList<SessionMemberDto>>
{
    public Guid SessionId { get; set; }
}

public class GetSessionMembersQueryHandler : IRequestHandler<GetSessionMembersQuery, IList<SessionMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetSessionMembersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IList<SessionMemberDto>> Handle(GetSessionMembersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.SessionMembers
            .Where(x => x.SessionId == request.SessionId&&x.DeletedAt==null)
            .ProjectToListAsync<SessionMemberDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}