using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using MediatR;

namespace CoreServer.Application.Files.Commands;

public class SaveUserFileCommand : IRequest<UserFile>
{
    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public FileType FileType { get; set; }

    public Stream FileStream { get; set; } = null!;
}

public class SaveUserFileCommandHandler : IRequestHandler<SaveUserFileCommand, UserFile>
{
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public SaveUserFileCommandHandler(IFileStorageService fileStorageService, IApplicationDbContext context)
    {
        _fileStorageService = fileStorageService;
        _context = context;
    }

    public async Task<UserFile> Handle(SaveUserFileCommand request, CancellationToken cancellationToken)
    {
        UserFile userFile = new UserFile
        {
            FileName = request.FileName, MimeType = request.MimeType, FileType = request.FileType
        };
        _context.UserFiles.Add(userFile);
        Result result = await _fileStorageService.SaveFileAsync(userFile, request.FileStream);
        if (result.Succeeded)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return userFile;
        }

        _context.UserFiles.Remove(userFile);
        throw new Exception(result.Errors.First());
    }
}