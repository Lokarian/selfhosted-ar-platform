using AutoMapper;
using AutoMapper.QueryableExtensions;
using CoreServer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries;

public class GetMyChatSessionsQuery : IRequest<IEnumerable<ChatSessionDto>>
{
}

public class GetMyChatSessionsQueryHandler : IRequestHandler<GetMyChatSessionsQuery, IEnumerable<ChatSessionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;

    public GetMyChatSessionsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IMapper mapper)
    {
        _context = context;
        _currentUserService = currentUserService;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ChatSessionDto>> Handle(GetMyChatSessionsQuery request,
        CancellationToken cancellationToken)
    {
        Guid userId = _currentUserService.User!.Id;
        List<ChatSessionDto> chatSessions = await _context.ChatSessions.Include(s => s.Messages)
            .Where(x => x.Members.Any(m => m.UserId == userId))
            .ProjectTo<ChatSessionDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        return chatSessions;
    }
}