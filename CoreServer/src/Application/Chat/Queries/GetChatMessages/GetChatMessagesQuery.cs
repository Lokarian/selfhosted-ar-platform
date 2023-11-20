using AutoMapper;
using AutoMapper.QueryableExtensions;
using CoreServer.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Chat.Queries.GetChatMessages;

public class GetChatMessagesQuery : IRequest<IList<ChatMessageDto>>
{
    public Guid SessionId { get; set; }
    public DateTime? From { get; set; }
    public int? Count { get; set; }
}

public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, IList<ChatMessageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetChatMessagesQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<IList<ChatMessageDto>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        return _context.ChatMessages
            .Include(m => m.Sender)
            .Where(m => m.SessionId == request.SessionId)
            .Where(m => request.From == null || m.SentAt < request.From)
            .OrderByDescending(m => m.SentAt)
            .Take(request.Count ?? 10)
            .ProjectTo<ChatMessageDto>(_mapper.ConfigurationProvider)
            .ToList();
    }
}