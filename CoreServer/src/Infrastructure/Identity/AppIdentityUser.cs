using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CoreServer.Infrastructure.Identity;

public class AppIdentityUser : IdentityUser
{
    public Guid? AppUserId { get; set; }
    public AppUser? AppUser { get; init; }
}