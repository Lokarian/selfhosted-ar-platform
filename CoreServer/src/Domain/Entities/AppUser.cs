namespace CoreServer.Domain.Entities;

public class AppUser:BaseEntity
{
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public Guid? ImageId { get; set; }
    public UserFile? Image { get; set; }
}