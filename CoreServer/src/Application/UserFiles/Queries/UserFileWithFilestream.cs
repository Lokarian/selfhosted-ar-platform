﻿using CoreServer.Domain.Entities;

namespace CoreServer.Application.UserFiles.Queries;

public class UserFileWithFilestream : IDisposable
{
    public UserFile UserFile { get; set; } = null!;
    public Stream FileStream { get; set; } = null!;

    public void Dispose()
    {
        FileStream.Dispose();
    }
}