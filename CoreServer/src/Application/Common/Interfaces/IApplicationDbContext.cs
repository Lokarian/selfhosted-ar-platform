using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<TodoList> TodoLists { get; }

    DbSet<TodoItem> TodoItems { get; }

    DbSet<AppUser> AppUsers { get; }
    DbSet<UserFile> UserFiles { get; }

    DbSet<UserSession> UserSessions { get; }
    DbSet<SessionMember> SessionMembers { get; }
    
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMember> ChatMembers { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    
    DbSet<VideoSession> VideoSessions { get; }
    DbSet<VideoMember> VideoMembers { get; }
    DbSet<VideoStream> VideoStreams { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}