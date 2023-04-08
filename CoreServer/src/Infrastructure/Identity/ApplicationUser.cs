using CoreServer.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace CoreServer.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? ImageId { get; set; }
    public UserFile? Image { get; set; }
}
