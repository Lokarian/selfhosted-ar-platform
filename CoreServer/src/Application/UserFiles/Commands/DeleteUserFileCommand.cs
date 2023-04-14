using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.Files.Commands;

public class DeleteUserFileCommand : IRequest
{
    public Guid Id { get; set; }
}

public class DeleteUserFileCommandHandler : IRequestHandler<DeleteUserFileCommand>
{
    private readonly IFileStorageService _fileStorageService;
    private readonly IApplicationDbContext _context;

    public DeleteUserFileCommandHandler(IFileStorageService fileStorageService, IApplicationDbContext context)
    {
        _fileStorageService = fileStorageService;
        _context = context;
    }

    public async Task<Unit> Handle(DeleteUserFileCommand request, CancellationToken cancellationToken)
    {
        var userFile = await _context.UserFiles.FindAsync(request.Id);
        if (userFile == null)
        {
            throw new NotFoundException(nameof(UserFile), request.Id);
        }

        _context.UserFiles.Remove(userFile);
        await _fileStorageService.DeleteFileAsync(userFile);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}