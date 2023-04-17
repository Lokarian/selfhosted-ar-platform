using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities;

namespace CoreServer.Application.User.Queries;

public class AppUserDto: IMapFrom<AppUser>
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Guid? ImageId { get; set; }
    public UserFile? Image { get; set; }
    public OnlineStatus OnlineStatus { get; set; }
}