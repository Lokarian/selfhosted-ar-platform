using AutoMapper;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.RPC;
using CoreServer.Application.RPC.common;
using CoreServer.Application.Video.Queries;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.EventHandlers;

public class VideoMemberUpdatedEventHandler : INotificationHandler<VideoMemberUpdatedEvent>
{
    private readonly IUserProxy<IRpcVideoService> _userProxy;
    private readonly IMapper _mapper;
    private readonly IApplicationDbContext _context;

    public VideoMemberUpdatedEventHandler(IUserProxy<IRpcVideoService> userProxy, IApplicationDbContext context, IMapper mapper)
    {
        _userProxy = userProxy;
        _context = context;
        _mapper = mapper;
    }

    public async Task Handle(VideoMemberUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var session = await _context.VideoSessions
            .Include(v => v.BaseSession)
            .ThenInclude(x => x.Members)
            .FirstOrDefaultAsync(x => x.Members.Any(m => m.Id == notification.VideoMember.Id), cancellationToken);
        var receivers = session!.BaseSession.Members.Select(x => x.UserId);
        var proxy = await _userProxy.Clients(receivers);
        await proxy.UpdateVideoMember(_mapper.Map<VideoMemberDto>(notification.VideoMember));
    }
}