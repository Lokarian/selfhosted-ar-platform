using CoreServer.Application.Common.Interfaces;

namespace CoreServer.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.UtcNow;
}
