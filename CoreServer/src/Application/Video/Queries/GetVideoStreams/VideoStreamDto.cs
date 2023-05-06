using CoreServer.Application.Common.Mappings;
using CoreServer.Domain.Entities.Video;

namespace CoreServer.Application.Video.Queries.Dtos;

public class VideoStreamDto:IMapFrom<VideoStream>
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StoppedAt { get; set; }
}