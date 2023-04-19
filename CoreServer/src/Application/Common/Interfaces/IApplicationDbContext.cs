using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Chat;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<AppUser> AppUsers { get; }
    DbSet<UserFile> UserFiles { get; }

    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    DbSet<ChatMember> ChatMembers { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}