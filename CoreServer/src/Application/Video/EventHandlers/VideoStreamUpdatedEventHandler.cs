using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Video.Queries.Dtos;
using CoreServer.Application.Video.Queries.GetMyVideoSessions;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.EventHandlers;

public class VideoStreamUpdatedEventHandler : INotificationHandler<VideoStreamUpdatedEvent>
{
    private readonly IUserProxy<IRpcVideoService> _userProxy;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _context;

    public VideoStreamUpdatedEventHandler(IUserProxy<IRpcVideoService> userProxy, IMapper mapper,
        IApplicationDbContext context)
    {
        _userProxy = userProxy;
        _mapper = mapper;
        _context = context;
    }

    public async Task Handle(VideoStreamUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var session = await _context.VideoSessions
            .Include(x => x.BaseSession)
            .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x=>x.Members.Any(m=>m.Streams.Any(s=>s.Id == notification.Stream.Id)), cancellationToken);
        if (session == null)
        {
            return;
        }
        var receivers = session.BaseSession.Members.Select(x => x.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateVideoStream(_mapper.Map<VideoStreamDto>(notification.Stream));
    }
}