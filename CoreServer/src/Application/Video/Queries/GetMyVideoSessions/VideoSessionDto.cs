using AutoMapper;
using CoreServer.Application.Common.Mappings;
using CoreServer.Application.Session.Queries.GetMySessions;
using CoreServer.Application.Video.Queries.Dtos;
using CoreServer.Domain.Entities.Chat;
using CoreServer.Domain.Entities.Session;
using CoreServer.Domain.Entities.Video;

namespace CoreServer.Application.Video.Queries.GetMyVideoSessions;

public class VideoSessionDto : IMapFrom<VideoSession>
{
    public Guid BaseSessionId { get; set; }
    public SessionDto? BaseSession { get; set; }
    
    public DateTime ReferencePoint { get; set; }
    public bool Active { get; set; }
    public IList<VideoMemberDto>? Members { get; set; } = new List<VideoMemberDto>();
    public IList<VideoStreamDto>? Streams { get; set; } = new List<VideoStreamDto>();

    public void Mapping(Profile profile)
    {
        profile.CreateMap<VideoSession, VideoSessionDto>()
            .ForMember(d => d.Active, opt => opt.MapFrom(s => s.Members.Any(x => x.DeletedAt == null)))
            .ForMember(d => d.Members, opt => opt.MapFrom(s => s.Members))
            .ForMember(x => x.Streams, opt => opt.MapFrom(s => s.Members.SelectMany(x => x.Streams)))
            .ForMember(x => x.BaseSession, opt => opt.Ignore());
    }
}