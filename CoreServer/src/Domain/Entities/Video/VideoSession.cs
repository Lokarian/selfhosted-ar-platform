namespace CoreServer.Domain.Entities.Video;

public class VideoSession:BaseEntity
{
    public DateTime ReferencePoint { get; set; }=DateTime.UtcNow;
}