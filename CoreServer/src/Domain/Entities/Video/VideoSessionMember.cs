namespace CoreServer.Domain.Entities.Video;

public class VideoSessionMember:BaseEntity
{
    public AppUser User { get; set; } = null!;
    public Guid UserId { get; set; }
    public bool Joined { get; set; }
    public IList<VideoStream> Streams { get; set; } = new List<VideoStream>();
}