﻿using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Video;
using CoreServer.Domain.Events.Video;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Video.Commands.CreateVideoSession;

public class CreateVideoSessionCommand : IRequest<VideoSession>
{
    public Guid SessionId { get; set; }
}

public class CreateVideoSessionCommandHandler : IRequestHandler<CreateVideoSessionCommand, VideoSession>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateVideoSessionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<VideoSession> Handle(CreateVideoSessionCommand request, CancellationToken cancellationToken)
    {
        var videoSession = new VideoSession { BaseSessionId = request.SessionId, };

        videoSession.AddDomainEvent(new VideoSessionCreatedEvent(videoSession));
        await _context.VideoSessions.AddAsync(videoSession, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return videoSession;
    }
}