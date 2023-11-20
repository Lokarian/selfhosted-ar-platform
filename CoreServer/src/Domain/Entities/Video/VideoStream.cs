namespace CoreServer.Domain.Entities.Video;

public class VideoStream:BaseEntity
{
    public VideoMember Owner { get; set; } = null!;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }=DateTime.UtcNow;
    public DateTime? StoppedAt { get; set; }
}
