using CoreServer.Application.Common.Exceptions;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.Files.Queries;

public class GetUserFileQuery : IRequest<UserFileWithFilestream>
{
    public Guid Id { get; set; }
}

public class GetUserFileQueryHandler : IRequestHandler<GetUserFileQuery, UserFileWithFilestream>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public GetUserFileQueryHandler(IFileStorageService fileStorageService, IApplicationDbContext context)
    {
        _fileStorageService = fileStorageService;
        _context = context;
    }

    public async Task<UserFileWithFilestream> Handle(GetUserFileQuery request, CancellationToken cancellationToken)
    {
        UserFile? userFile = await _context.UserFiles.FindAsync(request.Id);
        if (userFile == null)
        {
            throw new NotFoundException(nameof(UserFile), request.Id);
        }

        Stream fileStream = await _fileStorageService.GetFileAsync(userFile);
        return new UserFileWithFilestream { UserFile = userFile, FileStream = fileStream };
    }
}