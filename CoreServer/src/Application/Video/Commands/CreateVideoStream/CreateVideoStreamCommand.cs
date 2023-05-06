using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Chat;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.CreateVideoStream;

public class CreateVideoStreamCommand : IRequest<VideoStream>
{
    public Guid VideoStreamMemberId { get; set; }
}

public class CreateVideoStreamCommandHandler : IRequestHandler<CreateVideoStreamCommand, VideoStream>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public CreateVideoStreamCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<VideoStream> Handle(CreateVideoStreamCommand request, CancellationToken cancellationToken)
    {
        var videoSession = await _context.VideoSessions
            .Include(x => x.Members)
            .ThenInclude(x => x.Streams)
            .FirstOrDefaultAsync(s => s.Members.Any(m => m.BaseMember.UserId == _currentUserService.User!.Id),cancellationToken);

        var member = videoSession!.Members.FirstOrDefault(x => x.Id == request.VideoStreamMemberId);
        VideoStream stream = new() { OwnerId = member!.Id, CreatedAt = _dateTime.Now };
        _context.VideoStreams.Add(stream);

        stream.AddDomainEvent(new VideoStreamUpdatedEvent(stream));
        await _context.SaveChangesAsync(cancellationToken);
        return stream;
    }
}