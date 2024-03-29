﻿using System.Reflection;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Common;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.AR;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;
using CoreServer.Infrastructure.Common;
using CoreServer.Infrastructure.Identity;
using CoreServer.Infrastructure.Persistence.Configurations;
using CoreServer.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace CoreServer.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppIdentityUser>, IApplicationDbContext
{
    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;
    private readonly IMediator _mediator;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IMediator mediator,
        AuditableEntitySaveChangesInterceptor auditableEntitySaveChangesInterceptor)
        : base(options)
    {
        _mediator = mediator;
        _auditableEntitySaveChangesInterceptor = auditableEntitySaveChangesInterceptor;
    }

    public DbSet<TodoList> TodoLists => Set<TodoList>();

    public DbSet<TodoItem> TodoItems => Set<TodoItem>();

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<UserConnection> UserConnections => Set<UserConnection>();

    public DbSet<UserFile> UserFiles => Set<UserFile>();

    public DbSet<BaseSession> BaseSessions => Set<BaseSession>();
    public DbSet<SessionMember> SessionMembers => Set<SessionMember>();

    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();

    public DbSet<VideoSession> VideoSessions => Set<VideoSession>();
    public DbSet<VideoStream> VideoStreams => Set<VideoStream>();
    public DbSet<VideoMember> VideoMembers => Set<VideoMember>();
    
    public DbSet<ArSession> ArSessions => Set<ArSession>();
    public DbSet<ArMember> ArMembers => Set<ArMember>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<BaseEvent> domainEvents = _mediator.GetDomainEvents(this);
        var saveResult = await base.SaveChangesAsync(cancellationToken);
        await _mediator.DispatchDomainEvents(domainEvents);
        return saveResult;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("uuid-ossp");
        // apply generic BaseEntity configuration to all entities that inherit from BaseEntity
        // get all types that inherit from BaseEntity
        IEnumerable<IMutableEntityType> entityTypes = builder.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));

        // apply the BaseEntityConfiguration to each entity type
        foreach (IMutableEntityType entityType in entityTypes)
        {
            object? configuration = Activator.CreateInstance(
                typeof(BaseEntityConfiguration<>).MakeGenericType(entityType.ClrType));

            builder.ApplyConfiguration((dynamic)configuration);
        }

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(builder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntitySaveChangesInterceptor);
    }
}