using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Events;
using MediatR;

namespace CoreServer.Application.UserFiles.EventHandlers;

public class UserFileRemovedEventHandler : INotificationHandler<UserFileRemovedEvent>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public UserFileRemovedEventHandler(IApplicationDbContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(UserFileRemovedEvent notification, CancellationToken cancellationToken)
    {
        _context.UserFiles.Remove(notification.UserFile);
        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorageService.DeleteFileAsync(notification.UserFile);
    }
}