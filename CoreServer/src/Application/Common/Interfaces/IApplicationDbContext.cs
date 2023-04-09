using CoreServer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<AppUser> AppUsers { get; }
    DbSet<UserFile> UserFiles { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}