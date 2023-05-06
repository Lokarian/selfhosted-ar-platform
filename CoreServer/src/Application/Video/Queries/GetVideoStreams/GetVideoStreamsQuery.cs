using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Video.Queries.Dtos;
using CoreServer.Domain.Entities.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Queries.GetVideoStreams;

public class GetVideoStreamsQuery : IRequest<List<VideoStreamDto>>
{
    public Guid VideoSessionId { get; set; }
}

public class GetVideoStreamsQueryHandler : IRequestHandler<GetVideoStreamsQuery, List<VideoStreamDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetVideoStreamsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<VideoStreamDto>> Handle(GetVideoStreamsQuery request, CancellationToken cancellationToken)
    {
        return await _context.VideoStreams
            .Where(x => x.Owner.SessionId == request.VideoSessionId)
            .ProjectToListAsync<VideoStreamDto>(_mapper.ConfigurationProvider, cancellationToken);
    }
}