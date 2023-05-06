using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using MediatR;

namespace CoreServer.Application.Chat.Queries.GetChatMembers;

public class GetChatMembersQuery : IRequest<IEnumerable<ChatMemberDto>>
{
    public Guid SessionId { get; set; }
}

public class GetChatMembersQueryHandler : IRequestHandler<GetChatMembersQuery, IEnumerable<ChatMemberDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetChatMembersQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ChatMemberDto>> Handle(GetChatMembersQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.ChatMembers
            .Where(x => x.SessionId == request.SessionId)
            .ProjectToListAsync<ChatMemberDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}