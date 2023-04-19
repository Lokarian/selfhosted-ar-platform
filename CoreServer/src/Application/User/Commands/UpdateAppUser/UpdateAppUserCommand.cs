using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.User.Commands;

public class UpdateAppUserCommand : IRequest
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public UserFile? UserImage { get; set; }
}

public class UpdateAppUserCommandHandler : IRequestHandler<UpdateAppUserCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateAppUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(UpdateAppUserCommand request, CancellationToken cancellationToken)
    {
        AppUser? entity = await _context.AppUsers.Include(a => a.Image)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(AppUser), request.Id);
        }

        entity.UserName = request.UserName ?? entity.UserName;
        entity.Email = request.Email ?? entity.Email;
        //if image changed add UserFileRemovedEvent
        if (entity.Image != null && (request.UserImage == null || request.UserImage.Id != entity.Image.Id))
        {
            entity.AddDomainEvent(new UserFileRemovedEvent(entity.Image));
        }

        entity.Image = request.UserImage;

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}