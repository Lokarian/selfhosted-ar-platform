using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.StopVideoStream;

public class StopVideoStreamCommand: IRequest<VideoStream>
{
    public Guid VideoStreamId { get; set; }
}

public class StopVideoStreamCommandHandler : IRequestHandler<StopVideoStreamCommand, VideoStream>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;

    public StopVideoStreamCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService,
        IDateTime dateTime)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
    }

    public async Task<VideoStream> Handle(StopVideoStreamCommand request, CancellationToken cancellationToken)
    {
        var stream = await _context.VideoStreams.FirstOrDefaultAsync(x => x.Id == request.VideoStreamId, cancellationToken);
        if (stream == null)
        {
            throw new NotFoundException(nameof(VideoStream), _currentUserService.User!.Id);
        }

        stream.StoppedAt = _dateTime.Now;
        stream.AddDomainEvent(new VideoStreamUpdatedEvent(stream));
        await _context.SaveChangesAsync(cancellationToken);
        return stream;
    }
}