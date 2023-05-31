using AutoMapper;
using CoreServer.Application.AR.Queries.GetMyArSessions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Events.Ar;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.AR.Commands.CreateArSession;

public class CreateArSessionCommand : IRequest<ArSessionDto>
{
    public Guid SessionId { get; set; }
}

public class CreateArSessionCommandHandler : IRequestHandler<CreateArSessionCommand, ArSessionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public CreateArSessionCommandHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<ArSessionDto> Handle(CreateArSessionCommand request, CancellationToken cancellationToken)
    {
        BaseSession? baseSession = await _context.BaseSessions.Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        ArSession session = new ArSession { BaseSession = baseSession! };
        session.AddDomainEvent(new ArSessionCreatedEvent(session));
        _context.ArSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ArSessionDto>(session);
    }
}