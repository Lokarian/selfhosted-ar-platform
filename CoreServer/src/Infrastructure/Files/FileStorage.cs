using CoreServer.Application.Common.Interfaces;
using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace CoreServer.Infrastructure.Files;

public class FileStorage : IFileStorageService
{
    private readonly IConfiguration _configuration;

    public FileStorage(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<Result> SaveFileAsync(UserFile userFile, Stream fileStream)
    {
        try
        {
            //store file to file system from filetype
            var filePath = GetFilePath(userFile);
            using var file = File.Create(filePath);
            fileStream.CopyTo(file);
            return Task.FromResult(Result.Success());
        }
        catch (Exception e)
        {
            return Task.FromResult(Result.Failure(new[] { e.Message }));
        }
    }

    public Task<Stream> GetFileAsync(UserFile userFile)
    {
        var filePath = GetFilePath(userFile);
        var fileStream = File.Open(filePath, FileMode.Open);
        return Task.FromResult(fileStream as Stream);
    }

    public Task DeleteFileAsync(UserFile userFile)
    {
        var filePath = GetFilePath(userFile);
        File.Delete(filePath);
        return Task.CompletedTask;
    }

    private string GetFilePath(UserFile file)
    {
        var relativePath = this._configuration.GetSection("FileStorage").GetSection("FileTypeRelativePaths")
            .GetSection(file.FileType.ToString()).Value;
        if (relativePath == null)
        {
            relativePath = file.FileType.ToString();
        }

        return Path.Combine(this._configuration.GetSection("FileStorage").GetSection("StoragePath").Value, relativePath,
            $"{file.Id}_{file.FileName}");
    }
}