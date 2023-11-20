using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
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
    DbSet<UserConnection> UserConnections { get; }
    DbSet<UserFile> UserFiles { get; }

    DbSet<BaseSession> BaseSessions { get; }
    DbSet<SessionMember> SessionMembers { get; }
    
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ChatMember> ChatMembers { get; }
    DbSet<ChatMessage> ChatMessages { get; }
    
    DbSet<VideoSession> VideoSessions { get; }
    DbSet<VideoMember> VideoMembers { get; }
    DbSet<VideoStream> VideoStreams { get; }
    
    DbSet<ArSession> ArSessions { get; }
    DbSet<ArMember> ArMembers { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}