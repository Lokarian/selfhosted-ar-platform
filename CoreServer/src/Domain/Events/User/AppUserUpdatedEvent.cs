using MediatR;

namespace CoreServer.Domain.Events.User;

public class AppUserUpdatedEvent : INotification
{
    public AppUserUpdatedEvent(AppUser user)
    {
        User = user;
    }

    public AppUser User { get; }
}