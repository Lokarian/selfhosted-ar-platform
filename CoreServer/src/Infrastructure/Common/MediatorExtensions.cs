using System.Collections;
using CoreServer.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CoreServer.Infrastructure.Common;

public static class MediatorExtensions
{
    public static async Task DispatchDomainEvents(this IMediator mediator, IEnumerable<BaseEvent> events)
    {
        foreach (BaseEvent domainEvent in events)
        {
            await mediator.Publish(domainEvent);
        }
    }

    public static IEnumerable<BaseEvent> GetDomainEvents(this IMediator mediator, DbContext context)
    {
        IEnumerable<EntityWithEvents> entities = context.ChangeTracker
            .Entries<EntityWithEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);

        var entityWithEventsEnumerable = entities.ToList();
        List<BaseEvent> domainEvents = entityWithEventsEnumerable
            .SelectMany(e => e.DomainEvents)
            .ToList();

        entityWithEventsEnumerable.ForEach(e => e.ClearDomainEvents());
        return domainEvents;
    }
}