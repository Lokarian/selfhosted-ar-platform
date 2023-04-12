using CoreServer.Application.Common.Models;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<Result> SaveFileAsync(UserFile file, Stream fileStream);

    Task<Stream> GetFileAsync(UserFile file);

    Task DeleteFileAsync(UserFile file);
}