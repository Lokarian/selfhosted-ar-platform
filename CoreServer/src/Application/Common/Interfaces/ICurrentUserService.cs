using CoreServer.Domain.Entities;

namespace CoreServer.Application.Common.Interfaces;

public interface ICurrentUserService
{
    AppUser? User { get; set; }
    UserConnection? Connection { get; set; }
}