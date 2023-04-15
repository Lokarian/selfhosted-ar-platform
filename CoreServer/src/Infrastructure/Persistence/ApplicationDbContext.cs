﻿using System.Reflection;
using CoreServer.Application.Common.Interfaces;
using CoreServer.Domain.Common;
using CoreServer.Domain.Entities;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Infrastructure.Identity;
using CoreServer.Infrastructure.Persistence.Configurations;
using CoreServer.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<AppIdentityUser>, IApplicationDbContext
{
    private readonly IMediator _mediator;
    private readonly AuditableEntitySaveChangesInterceptor _auditableEntitySaveChangesInterceptor;

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
    
    public DbSet<UserFile> UserFiles => Set<UserFile>();
    
    public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
    
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("uuid-ossp");
        // apply generic BaseEntity configuration to all entities that inherit from BaseEntity
        // get all types that inherit from BaseEntity
        var entityTypes = builder.Model.GetEntityTypes()
            .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType));

        // apply the BaseEntityConfiguration to each entity type
        foreach (var entityType in entityTypes)
        {
            var configuration = Activator.CreateInstance(
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _mediator.DispatchDomainEvents(this);

        return await base.SaveChangesAsync(cancellationToken);
    }
}